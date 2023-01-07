from enum import Enum
from voice_util import VoiceUtilClass

script_path = 'InDevelopment/ManualUpdates/0.utf'
script_with_comments = "script_with_comments.txt"

# I've adapted this from https://github.com/drojf/umineko_python_scripts/tree/master/Fix_Automatic_Voice_Lines
voice_database_path = 'tools/voice-length-database/question.pickle'
voice_util_class = VoiceUtilClass(script_path=script_path, voice_length_pickle_path=voice_database_path)


with open(script_with_comments, encoding='utf-8') as f:
    lines = f.readlines()

# def processLine(line, lineLanguage):
#     length

class LineLanguage(Enum):
    NONE = 1
    EN = 2
    JP = 3

last_jp_length = None
last_en_length = None

for line in lines:
        lineLanguage = LineLanguage.NONE

        if '; SOURCE_LINE_JP' in line:
            lineLanguage = LineLanguage.JP
            last_jp_length = voice_util_class.try_get_voice_length_of_line(line.replace('dwave_jp', 'dwave'), show_output=False)
        elif '; SOURCE_LINE_EN' in line:
            lineLanguage = LineLanguage.EN
            last_en_length = voice_util_class.try_get_voice_length_of_line(line.replace('dwave_eng', 'dwave'), show_output=False)



