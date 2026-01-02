
import re


def fix_voicedelay(lines):
    emit_lines = []
    last_se_line = None

    count = 0
    for line in lines:
        line_to_emit = line
        if 'voicedelay' in line and last_se_line:
            count += 1
            print("prev:", last_se_line)
            print("next:", line)
            emit_lines.append(last_se_line)
        elif last_se_line is not None:
            emit_lines.append(last_se_line)


        if line.strip() == '':
            pass
        elif re.match('se\\d.*\n', line):
            last_se_line = line
        else:
            last_se_line = None

        if last_se_line is None:
            emit_lines.append(line_to_emit)

    print(f"Got {count} matches")

    return emit_lines


def fix_script(script_path):
    
    with open(script_path, encoding='utf-8') as f:
        lines = f.readlines()

    new_lines = fix_voicedelay(lines)

    with open('fix_lines_test.utf', 'w', encoding='utf-8') as out:
        out.writelines(new_lines)

    # matches = re.findall('se\\d.*\n.*voicedelay[^:\n]*', whole_file)

    # count = 0
    # for match in matches:
    #     print('----')
    #     print(match)
    #     count += 1

    # print(f"Got {count} matches")

fix_script('InDevelopment/ManualUpdates/0.utf')