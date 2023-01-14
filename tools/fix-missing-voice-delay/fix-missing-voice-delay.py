from enum import Enum
import pickle
import re
from voice_util import VoiceUtilClass
import config

script_path = 'InDevelopment/ManualUpdates/0.utf'
script_with_comments = "script_with_comments.txt"

match_dict = {}

regex_lineEnding = re.compile(r'[@\\//]+$')

def check_line_starts_with_clickwait(line):
    trimmed_line = line.lower().replace('langen', '').replace('langjp', '').replace('!sd', '').replace('^', '').replace(':', '')
    return trimmed_line.startswith('@') or trimmed_line.startswith('\\')


def line_is_english(line):
    english = 'langen' in line
    japanese = 'langjp' in line

    if not english and not japanese:
        return None

    if english and japanese:
        raise Exception("Error: line has both language markers: {line}")

    return english

def preprocess_line(line):
    # remove comment
    line = line.split(";", 1)[0]

    # Strip all whitespace before doing line ending check as we don't care about it
    return re.sub(r"\s+", "", line, flags=re.UNICODE)

class LineEndingType(Enum):
    NONE = 1
    CLICKWAIT = 2
    CONTINUE = 3

def get_line_ending(line):
    line = preprocess_line(line)
    line_noWhiteSpace = re.sub(r"\s+", "", line, flags=re.UNICODE)

    match = regex_lineEnding.search(line_noWhiteSpace)

    # Skip lines which don't have any clickwait markers at all at the end
    if not match:
        return LineEndingType.NONE

    lineEnding = match[0]
    match_dict[lineEnding] = None

    # Skip lines with clickwaits at the end (any character other than / at the end)
    if len(lineEnding.strip('/')) != 0:
        return LineEndingType.CLICKWAIT

    return LineEndingType.CONTINUE

def line_afterwards_needs_voice_delay(line):
    line_ending_type = get_line_ending(line)

    if line_ending_type != LineEndingType.CONTINUE:
        return False

    # Skip lines with no voices on it (this will give some false positives but this is OK)
    if 'dwave' not in line:
        return False

    return True


with open(script_path, encoding='utf-8') as f:
    lines = f.readlines()

next_jp_needs_voice_delay = False
next_en_needs_voice_delay = False

output = []

match_count = 0
fix_count = 0

with open(script_with_comments, 'w', encoding='utf-8') as script_with_comments:
    for lineIndex, line in enumerate(lines):
        # Afaik voicewait applies to both languages, so just clear both flags
        if line.lower().startswith('voicewait'):
            next_en_needs_voice_delay = False
            next_jp_needs_voice_delay = False

        is_english = line_is_english(line)

        if is_english is None:
            # Skip lines which don't have langen or langjp
            script_with_comments.write(line)
        else:
            line_needs_voice_delay = False

            if next_en_needs_voice_delay and is_english:
                next_en_needs_voice_delay = False
                line_needs_voice_delay = True
            elif next_jp_needs_voice_delay and not is_english:
                next_jp_needs_voice_delay = False
                line_needs_voice_delay = True

            # Don't apply voicedelay (clear the flag!) if
            #  - there is already voicedelay on that line
            #  - there is a noclear_cw on that line
            #  - the line starts with a clickwait, ignoring langen/langjp/sd!
            #  - there is a voicewait
            if line_needs_voice_delay:
                if 'voicedelay' in line.lower() or 'noclear_cw' in line.lower() or check_line_starts_with_clickwait(line):
                    line_needs_voice_delay = False

            # If you need to insert a voice delay but the line is not suitable, then postpone it to the next line
            if line_needs_voice_delay and 'dwave' not in line:
                line_needs_voice_delay = False

                if get_line_ending(line) == LineEndingType.CLICKWAIT:
                    # If the line ends with clickwait, the voicedelay is not needed
                    # print(f"Cancelling voicedelay due to line {line}")
                    pass
                else:
                    next_en_needs_voice_delay = is_english
                    next_jp_needs_voice_delay = not is_english


            if line_afterwards_needs_voice_delay(line):
                match_count += 1
                output.append(line)
                if is_english:
                    next_en_needs_voice_delay = True
                else:
                    next_jp_needs_voice_delay = True

            if line_needs_voice_delay:
                script_with_comments.write(f"{config.VDELAY_MARKER}{line}")
                fix_count += 1
            else:
                output_line = line
                script_with_comments.write(output_line)


print(match_dict)

print(f"Got {match_count} matches")
print(f"Will fix {fix_count} lines")


with open("out.txt", 'w', encoding='utf-8') as f:
    f.writelines(output)
