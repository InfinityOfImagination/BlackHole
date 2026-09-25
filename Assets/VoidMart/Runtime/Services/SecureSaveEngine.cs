using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace VoidMart.Services
{
    /// <summary>
    /// Encrypted, atomic local persistence as specified in section 8.
    ///
    /// Primary path is AES-256-GCM with a PBKDF2 key derived from the device identifier, exactly
    /// as the spec describes.  Because Unity's scripting backends do not all ship a working
    /// <see cref="AesGcm"/> (WebGL and some Mono builds throw <see cref="PlatformNotSupportedException"/>),
    /// the engine transparently falls back to AES-256-CBC + HMAC-SHA256 encrypt-then-MAC, which
    /// provides the same authenticated-encryption guarantee.  A format byte in the header records
    /// which path wrote the file so old saves keep loading after a platform switch.
    /// </summary>
    public static class SecureSaveEngine
    {
        const byte FormatGcm = 0xA1;
        const byte FormatCbcHmac = 0xA2;
        const int KeyBytes = 32;   // 256-bit
        const int NonceBytes = 12; // 96-bit GCM nonce
        const int TagBytes = 16;   // 128-bit auth tag
        const int Pbkdf2Iterations = 10000;

        static readonly byte[] Salt = Encoding.UTF8.GetBytes("VOID_MART_v1");
        static byte[] s_CachedKey;
        static bool s_GcmUnavailable;

        public static bool LastLoadFailed { get; private set; }

        static byte[] DeriveKey()
        {
            if (s_CachedKey != null) return s_CachedKey;

            // Device-specific persistent key: prevents copying a save between devices.
            string seed = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(seed) || seed == SystemInfo.unsupportedIdentifier)
                seed = Application.identifier + "|fallback-device-seed";

            using (var pbkdf2 = new Rfc2898DeriveBytes(seed, Salt, Pbkdf2Iterations, HashAlgorithmName.SHA256))
            {
                s_CachedKey = pbkdf2.GetBytes(KeyBytes);
            }
            return s_CachedKey;
        }

        static byte[] RandomBytes(int count)
        {
            var bytes = new byte[count];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Builds an AesGcm instance across API profiles: .NET 8 requires the (key, tagSize)
        /// constructor while .NET Standard 2.1 only exposes (key).
        /// </summary>
        static AesGcm CreateGcm(byte[] key)
        {
            var type = typeof(AesGcm);
            var withTag = type.GetConstructor(new[] { typeof(byte[]), typeof(int) });
            if (withTag != null) return (AesGcm)withTag.Invoke(new object[] { key, TagBytes });
            var basic = type.GetConstructor(new[] { typeof(byte[]) });
            if (basic != null) return (AesGcm)basic.Invoke(new object[] { key });
            throw new PlatformNotSupportedException("No usable AesGcm constructor");
        }

        // ------------------------------------------------------------------ write

        public static bool SaveEncrypted(string path, string plainText)
        {
            string tempPath = path + ".tmp";
            try
            {
                byte[] payload = Encode(plainText);

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(tempPath, payload);

                // Atomic-ish replace so a kill mid-write can never truncate a good save.
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoidMart] Save failed: {e.Message}");
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* ignored */ }
                return false;
            }
        }

        static byte[] Encode(string plainText)
        {
            byte[] key = DeriveKey();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            if (!s_GcmUnavailable)
            {
                try
                {
                    byte[] nonce = RandomBytes(NonceBytes);
                    byte[] cipher = new byte[plainBytes.Length];
                    byte[] tag = new byte[TagBytes];
                    using (var gcm = CreateGcm(key)) gcm.Encrypt(nonce, plainBytes, cipher, tag);
                    return Pack(FormatGcm, nonce, tag, cipher);
                }
                catch (Exception e)
                {
                    s_GcmUnavailable = true;
                    Debug.LogWarning($"[VoidMart] AES-GCM unavailable ({e.GetType().Name}); using AES-CBC+HMAC.");
                }
            }

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.IV = RandomBytes(16);
                byte[] cipher;
                using (var encryptor = aes.CreateEncryptor())
                    cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                byte[] mac;
                using (var hmac = new HMACSHA256(key)) mac = hmac.ComputeHash(Concat(aes.IV, cipher));
                return Pack(FormatCbcHmac, aes.IV, mac, cipher);
            }
        }

        static byte[] Pack(byte format, byte[] iv, byte[] tag, byte[] cipher)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(format);
                writer.Write(iv.Length); writer.Write(iv);
                writer.Write(tag.Length); writer.Write(tag);
                writer.Write(cipher.Length); writer.Write(cipher);
                writer.Flush();
                return stream.ToArray();
            }
        }

        static byte[] Concat(byte[] a, byte[] b)
        {
            var result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }

        // ------------------------------------------------------------------ read

        public static string LoadEncrypted(string path)
        {
            LastLoadFailed = false;
            if (!File.Exists(path)) return null;

            try
            {
                byte[] raw = File.ReadAllBytes(path);
                using (var stream = new MemoryStream(raw))
                using (var reader = new BinaryReader(stream))
                {
                    byte format = reader.ReadByte();
                    byte[] iv = reader.ReadBytes(reader.ReadInt32());
                    byte[] tag = reader.ReadBytes(reader.ReadInt32());
                    byte[] cipher = reader.ReadBytes(reader.ReadInt32());
                    byte[] key = DeriveKey();

                    if (format == FormatGcm)
                    {
                        byte[] plain = new byte[cipher.Length];
                        using (var gcm = CreateGcm(key)) gcm.Decrypt(iv, cipher, tag, plain);
                        return Encoding.UTF8.GetString(plain);
                    }

                    if (format == FormatCbcHmac)
                    {
                        byte[] expected;
                        using (var hmac = new HMACSHA256(key)) expected = hmac.ComputeHash(Concat(iv, cipher));
                        if (!FixedTimeEquals(expected, tag))
                            throw new CryptographicException("Save authentication failed (tampered file).");

                        using (var aes = Aes.Create())
                        {
                            aes.KeySize = 256;
                            aes.Key = key;
                            aes.Mode = CipherMode.CBC;
                            aes.Padding = PaddingMode.PKCS7;
                            aes.IV = iv;
                            using (var decryptor = aes.CreateDecryptor())
                            {
                                byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                                return Encoding.UTF8.GetString(plain);
                            }
                        }
                    }

                    throw new InvalidDataException("Unknown save format 0x" + format.ToString("X2"));
                }
            }
            catch (Exception e)
            {
                LastLoadFailed = true;
                Debug.LogError($"[VoidMart] Save load failed ({e.GetType().Name}: {e.Message}). Starting a fresh profile.");
                return null;
            }
        }

        static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        /// <summary>Plain-text mirror used by the editor Master Tool for inspection.</summary>
        public static void WriteDebugMirror(string path, string plainText)
        {
            try { File.WriteAllText(path, plainText); }
            catch (Exception e) { Debug.LogWarning("[VoidMart] Debug mirror failed: " + e.Message); }
        }
    }
}
