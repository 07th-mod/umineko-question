# This file is a modified version of check_database.py from https://github.com/drojf/umineko_python_scripts/tree/master/Fix_Automatic_Voice_Lines

import os
import pickle
import re
import VoiceLineUtils


# seventh_script_path = r'C:\drojf\large_projects\umineko\umineko_question_repo\InDevelopment\ManualUpdates\0.utf'
# output_script_path =  r'C:\drojf\large_projects\umineko\umineko_question_repo\InDevelopment\ManualUpdates\0_fixed_3.utf'

get_voice_regex = re.compile(r'dwave\s+\d+\s*,\s*(\w+)', flags=re.IGNORECASE)

voiceDelayRegex = re.compile(r'voicedelay (\d+)', flags=re.IGNORECASE)

fullOggPath = re.compile(r'voice[\\/]\d\d[^.]*.ogg', flags=re.IGNORECASE)






class VoiceUtilClass:
    def __init__(self, script_path, voice_length_pickle_path):
        # load entire file in, and load aliases (specifically for voice aliases)
        with open(script_path, encoding='utf-8') as seventh_script:
            entire_file_all_lines = seventh_script.readlines()

        self.aliasMapping = VoiceLineUtils.AliasMapping()
        for line in entire_file_all_lines:
            self.aliasMapping.parse(line)

        #load pickled index of voice file lengths
        with open(voice_length_pickle_path, 'rb') as f:
            self.voice_file_length_dict = pickle.load(f)


    def try_get_voice_length_of_line(self, line, show_output = True):
        voice_alias_or_path = self.try_get_voice_on_line(line, show_output)

        voice_file_path = None

        if '.ogg' in voice_alias_or_path:
            voice_file_path = voice_alias_or_path
        else:
            voice_file_path = self.aliasMapping.get(voice_alias_or_path)

        if show_output:
            print('Looking up [{}]...'.format(voice_file_path))

        return self.get_audio_length_as_script_delay(voice_file_path)

    # there are two types of voiced lines - ones which use alias and ones which are hardcoded.
    def try_get_voice_on_line(self, line, show_output = True):
        all_voices = re.findall(get_voice_regex, line)
        all_ogg_files = re.findall(fullOggPath, line)

        if all_voices and all_ogg_files:
            raise Exception("a mix of ogg files and raw paths on same line! this case is not handled properly", line)

        if len(all_voices) > 0:
            if show_output:
                print('got voice alias: ',  all_voices[-1])
            return all_voices[-1]

        if len(all_ogg_files) > 0:
            if show_output:
                print('got ogg path', all_ogg_files[-1])
            return all_ogg_files[-1]

        raise Exception("couldn't find voice file in line '{}'".format(line.strip()))

    def get_audio_length_as_script_delay(self, filepath):
        normalized_path = os.path.normpath(filepath)
        # print(filepath, normalized_path)

        return VoiceLineUtils.convertVoiceSecondsToScriptDelay(self.voice_file_length_dict[normalized_path])

    def line_is_voice_line(line):
        all_voices = re.findall(get_voice_regex, line)
        all_ogg_files = re.findall(fullOggPath, line)

        return all_voices or all_ogg_files


# last_voice_line = None

# output_all_lines = []

# for line in entire_file_all_lines:
#     new_line = line

#     # If a 'insert_delay_here' is found, replace it with the last voice delay encountered
#     if re.search(voiceDelayRegex, line):
#         # print('prev voice line:    ' + last_voice_line.strip())
#         # print('current_line: ' + line.strip())

#         last_alias = try_get_voice_on_line(last_voice_line, show_output=False)
#         if 'dwave' not in line:
#             print('WARNING: not fixing line {} due no vocies on this line {}'.format(line.strip(), last_voice_line.strip()))
#         elif last_alias in voice_ignore_list.voice_ignore_list:
#             print('not fixing line {} due to MANUALLY ignored line {}'.format(line.strip(), last_voice_line.strip()))
#         else:
#             script_voice_length = try_get_voice_length_of_line(last_voice_line, aliasMapping, show_output=False)

#             #print('last voice path:   ' + alias_path)
#             # print('last voice length: ' + str(script_voice_length))
#             # print()

#             new_line = re.sub(voiceDelayRegex, 'voicedelay {}'.format(script_voice_length), line)

#     # scan each line for voices. Record the last voice on the line.
#     if line_is_voice_line(line):
#         last_voice_line = line

#     # output to file
#     if line != new_line:
#         print('CHANGE MADE:')
#         print('prev voice line: ', last_voice_line.strip())
#         print('old line: ', line.strip())
#         print('new line: ', new_line.strip())

#     output_all_lines.append(new_line)

# with open(output_script_path, 'w',encoding=conf.encoding) as output_file:
#     output_file.writelines(output_all_lines)