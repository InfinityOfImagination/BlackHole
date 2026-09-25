#!/usr/bin/env python3
"""Structural sanity checks for the Void Mart C# sources.

There is no .NET toolchain in this container, so this catches the classes of error a
compiler would: unbalanced delimiters, stray non-ASCII in code, duplicate members,
and types used without a matching `using` for the namespace that declares them.
"""
import re, sys, os
from collections import defaultdict

ROOT = sys.argv[1] if len(sys.argv) > 1 else "Assets/VoidMart"

def strip_code(src):
    """Replace comments/strings/chars with spaces, preserving offsets."""
    out = list(src)
    i, n = 0, len(src)
    while i < n:
        c = src[i]
        if c == '/' and i + 1 < n and src[i+1] == '/':
            while i < n and src[i] != '\n':
                out[i] = ' '; i += 1
        elif c == '/' and i + 1 < n and src[i+1] == '*':
            while i < n and not (src[i] == '*' and i + 1 < n and src[i+1] == '/'):
                if src[i] != '\n': out[i] = ' '
                i += 1
            if i < n: out[i] = ' '; out[i+1] = ' '; i += 2
        elif c == '@' and i + 1 < n and src[i+1] == '"':
            out[i] = ' '; out[i+1] = ' '; i += 2
            while i < n:
                if src[i] == '"':
                    if i + 1 < n and src[i+1] == '"':
                        out[i] = out[i+1] = ' '; i += 2; continue
                    out[i] = ' '; i += 1; break
                if src[i] != '\n': out[i] = ' '
                i += 1
        elif c == '"':
            out[i] = ' '; i += 1
            while i < n and src[i] != '"':
                if src[i] == '\\':
                    out[i] = ' '
                    if i + 1 < n: out[i+1] = ' '
                    i += 2; continue
                if src[i] != '\n': out[i] = ' '
                i += 1
            if i < n: out[i] = ' '; i += 1
        elif c == "'":
            j = i + 1
            depth = 0
            while j < n and depth < 4:
                if src[j] == '\\': j += 2; depth += 1; continue
                if src[j] == "'": break
                j += 1; depth += 1
            if j < n and src[j] == "'":
                for k in range(i, j + 1): out[k] = ' '
                i = j + 1
            else:
                i += 1
        else:
            i += 1
    return ''.join(out)

files = []
for dirpath, _, names in os.walk(ROOT):
    for nm in names:
        if nm.endswith('.cs'):
            files.append(os.path.join(dirpath, nm))
files.sort()

errors, warnings = [], []
type_ns = defaultdict(set)     # type name -> namespaces declaring it
enum_members = set()
file_info = {}

TYPE_DECL = re.compile(
    r'\b(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|new|\s)*'
    r'\b(class|struct|interface|enum)\s+([A-Za-z_]\w*)')
NS_DECL = re.compile(r'\bnamespace\s+([A-Za-z_][\w.]*)')

for path in files:
    raw = open(path, encoding='utf-8').read()
    code = strip_code(raw)
    ns = NS_DECL.search(code)
    ns = ns.group(1) if ns else ''
    usings = set(re.findall(r'\busing\s+(?:static\s+)?([A-Za-z_][\w.]*)\s*;', code))
    decls = []
    for m in TYPE_DECL.finditer(code):
        prefix = code[:m.start()]
        depth = prefix.count('{') - prefix.count('}')
        decls.append((m.group(1), m.group(2), depth))
    # Only top-level types (depth 1 == directly inside the namespace) participate in
    # the using check; nested types are always reached through their owner.
    for kind, name, depth in decls:
        if depth <= 1:
            type_ns[name].add(ns)
    file_info[path] = dict(raw=raw, code=code, ns=ns, usings=usings, decls=decls)

    # Enum member names are not type references.
    for m in re.finditer(r'\benum\s+[A-Za-z_]\w*\s*(?::\s*\w+\s*)?\{([^}]*)\}', code):
        for member in re.findall(r'\b([A-Za-z_]\w*)\b', m.group(1)):
            enum_members.add(member)

for path, info in file_info.items():
    code, raw = info['code'], info['raw']

    # 1. delimiter balance
    stack = []
    line = 1
    pairs = {')': '(', ']': '[', '}': '{'}
    for idx, ch in enumerate(code):
        if ch == '\n': line += 1
        elif ch in '([{': stack.append((ch, line))
        elif ch in ')]}':
            if not stack or stack[-1][0] != pairs[ch]:
                errors.append(f"{path}:{line}: unbalanced '{ch}'")
                break
            stack.pop()
    else:
        if stack:
            ch, ln = stack[-1]
            errors.append(f"{path}:{ln}: unclosed '{ch}'")

    # 2. non-ASCII outside strings/comments
    for m in re.finditer(r'[^\x00-\x7F]', code):
        ln = code[:m.start()].count('\n') + 1
        errors.append(f"{path}:{ln}: non-ASCII char {m.group(0)!r} in code")

    # 3. duplicate field/property names per type body (shallow: whole file)
    fields = re.findall(r'\[SerializeField\][^;{]*?\b([A-Za-z_]\w*)\s*(?:;|=)', code)
    seen = defaultdict(int)
    for f in fields: seen[f] += 1
    for f, c in seen.items():
        if c > 1:
            warnings.append(f"{path}: field '{f}' declared {c} times")

    # 4. types used without a using for their namespace
    ns, usings = info['ns'], info['usings']
    visible = set(usings) | {ns}
    # parent namespaces are visible from within a namespace
    parts = ns.split('.')
    for i in range(1, len(parts)):
        visible.add('.'.join(parts[:i]))
    local_names = {n for _, n, _ in info['decls']}
    tokens = set(re.findall(r'\b[A-Z]\w*\b', code))
    for tok in sorted(tokens):
        if tok in local_names or tok not in type_ns or tok in enum_members:
            continue
        owners = type_ns[tok]
        if owners & visible:
            continue
        # qualified use?  e.g. VoidMart.Services.EconomyService or Services.EconomyService
        if re.search(r'\.\s*' + re.escape(tok) + r'\b', code):
            qualified_ok = False
            for owner in owners:
                tail = owner.split('.')[-1]
                if re.search(r'\b' + re.escape(tail) + r'\s*\.\s*' + re.escape(tok) + r'\b', code):
                    qualified_ok = True; break
            if qualified_ok:
                continue
        errors.append(f"{path}: type '{tok}' (declared in {', '.join(sorted(owners))}) used without a matching using")

print(f"scanned {len(files)} files, {len(type_ns)} declared types")
for w in warnings: print("WARN ", w)
for e in errors: print("ERROR", e)
print(f"\n{len(errors)} errors, {len(warnings)} warnings")
sys.exit(1 if errors else 0)
