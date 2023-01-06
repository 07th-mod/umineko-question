import re

script_path = 'InDevelopment/ManualUpdates/0.utf'

# This regex may add some voice delays when not needed, but
# regex_noClickWaitLine = re.compile(r'dwave[^@]*\/\s*$')

match_dict = {}

regex_lineEnding = re.compile(r'[@\\//]+$')


def line_is_english(line):
    english = 'langen' in line
    japanese = 'langjp' in line

    if not english and not japanese:
        raise Exception("Error: line missing language marker: {line}")

    if english and japanese:
        raise Exception("Error: line has both language markers: {line}")

    return english


def line_afterwards_needs_voice_delay(line):
    # Strip all whitespace before doing line ending check as we don't care about it
    line_noWhiteSpace = re.sub(r"\s+", "", line, flags=re.UNICODE)

    match = regex_lineEnding.search(line_noWhiteSpace)

    # Skip lines which don't have any clickwait markers at all at the end
    if not match:
        return False

    lineEnding = match[0]
    match_dict[lineEnding] = None

    # Skip lines with clickwaits at the end (any character other than / at the end)
    if len(lineEnding.strip('/')) != 0:
        return False

    # Skip lines with no voices on it (this will give some false positives but this is OK)
    if 'dwave' not in line:
        return False

    return True


with open(script_path, encoding='utf-8') as f:
    lines = f.readlines()

output = []

match_count = 0
for lineIndex, line in enumerate(lines):
    # Skip comments
    if line.lstrip().startswith(';'):
        continue

    if line_afterwards_needs_voice_delay(line):
        match_count += 1
        output.append(line)


print(match_dict)

print(f"Got {match_count} matches")


with open("out.txt", 'w', encoding='utf-8') as f:
    f.writelines(output)
