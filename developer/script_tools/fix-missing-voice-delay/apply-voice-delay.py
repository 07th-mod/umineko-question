from enum import Enum
import re
from voice_util import VoiceUtilClass
import config

answer_arcs = False

script_path = 'InDevelopment/ManualUpdates/0.utf'
script_with_comments = "script_with_comments.txt"
output_file = "output_script.txt"
voice_database_path = 'tools/voice-length-database/question.pickle'

if answer_arcs:
    script_path = '../umineko-answer/0.utf'
    script_with_comments = "script_with_comments_answer.txt"
    output_file = "output_script_answer.txt"
    voice_database_path = 'tools/voice-length-database/answer.pickle'

# I've adapted this from https://github.com/drojf/umineko_python_scripts/tree/master/Fix_Automatic_Voice_Lines
voice_util_class = VoiceUtilClass(script_path=script_path, voice_length_pickle_path=voice_database_path)


with open(script_with_comments, encoding='utf-8') as f:
    lines = f.readlines()

class LineLanguage(Enum):
    NONE = 1
    EN = 2
    JP = 3

last_jp_length = None
last_en_length = None

vdelay_count = 0

with open(output_file, 'w', encoding='utf-8') as f:
    for line in lines:
        output_line = line

        lineLanguage = LineLanguage.NONE

        if 'langjp' in line:
            lineLanguage = LineLanguage.JP
        elif 'langen' in line:
            lineLanguage = LineLanguage.EN

        # apply the VDelay first
        if config.VDELAY_MARKER in line:
            if lineLanguage == LineLanguage.NONE:
                raise Exception(f"Error - need vdelay on line [{line}] with no language")
            elif lineLanguage == LineLanguage.JP:
                if last_jp_length == None:
                    raise Exception(f"line {line} needs vdelay but wasnt preceded with source length line!")
                length = last_jp_length
                last_jp_length = None
            elif lineLanguage == LineLanguage.EN:
                if last_en_length == None:
                    raise Exception(f"line {line} needs vdelay but wasnt preceded with source length line!")
                length = last_en_length
                last_en_length = None

            output_line = line
            output_line = output_line.replace(config.VDELAY_MARKER, '')
            output_line, replace_count = re.subn('((langjp)|(langen))', f'\\1:voicedelay {length}:', output_line)
            output_line = output_line.replace('::', ':')

            if replace_count != 1:
                raise Exception(f"Couldn't insert voicedelay on line {line}")

            vdelay_count += 1

        f.write(output_line)

        # Then check if the line has a dwave and update it
        if 'dwave_jp' in line or 'dwave_eng' in line:
            if 'defsub' not in line and '*dwave_jp' not in line and '*dwave_en' not in line:
                try:
                    if lineLanguage == LineLanguage.JP:
                        last_jp_length = None
                        last_jp_length = voice_util_class.try_get_voice_length_of_line(line.replace('dwave_jp', 'dwave'), show_output=False)
                    elif lineLanguage == LineLanguage.EN:
                        last_en_length = None
                        last_en_length = voice_util_class.try_get_voice_length_of_line(line.replace('dwave_eng', 'dwave'), show_output=False)
                    else:
                        raise Exception(f"Error - dwave on line {line} but language not set!")
                except KeyError as e:
                    # TODO: emit proper custom Exception in try_get_voice_length_of_line
                    # Note: above try block will set the last_jp/en_length to None if exception raised before voice can be retrieved
                    # print(f"Failed to extract dwave length on line {line}")
                    pass

print(f"Successfuly inserted {vdelay_count} voicedelays")