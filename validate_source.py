from pathlib import Path
import re
import sys

root = Path(__file__).parent
sources = sorted(p for p in root.rglob('*.cs') if 'bin' not in p.parts and 'obj' not in p.parts)
errors = []

if not sources:
    print('no C# sources found next to this script')
    sys.exit(1)

for path in sources:
    text = path.read_text(encoding='utf-8')
    stripped = re.sub(r'@?"(?:""|\\.|[^"\\])*"', '""', text, flags=re.S)

    if '//' in stripped:
        errors.append(f'{path.name}: line comment found')

    if '/*' in stripped or '*/' in stripped:
        errors.append(f'{path.name}: block comment found')

    opens = stripped.count('{')
    closes = stripped.count('}')
    if opens != closes:
        errors.append(f'{path.name}: brace mismatch {opens} != {closes}')

    round_opens = stripped.count('(')
    round_closes = stripped.count(')')
    if round_opens != round_closes:
        errors.append(f'{path.name}: parenthesis mismatch {round_opens} != {round_closes}')

all_text = '\n'.join(path.read_text(encoding='utf-8') for path in sources)
required = [
    'RequiredExiledVersion => new Version(9, 14, 2)',
    'public sealed class Plugin : Plugin<Config>',
    'CommandHandler(typeof(RemoteAdminCommandHandler))',
    'CommandHandler(typeof(GameConsoleCommandHandler))',
    'CommandHandler(typeof(ClientCommandHandler))',
    'SSPlaintextSetting',
    'SSDropdownSetting',
    'SSButton',
    'DisplayNickname',
    'clans.json',
]

for marker in required:
    if marker not in all_text:
        errors.append('missing marker: ' + marker)

if errors:
    print('\n'.join(errors))
    sys.exit(1)

print(f'SOURCE VALIDATION OK: {len(sources)} files, no comments, balanced braces, required markers present.')
print('This is a source layout check, not a C# compilation.')
