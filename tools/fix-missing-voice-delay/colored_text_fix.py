import re


script_path = '../umineko-answer/0.utf'
output_path = '../umineko-answer/0.fixcolor.utf'

def get_voicedelay(line):
    result = re.search('(voicedelay\s+\d+)', line)
    if result:
        return result.group(1)
    else:
        raise Exception(f"Couldn't find voicedelay on line {line}")

def remove_voicedelay_and_tidy(line):
    return re.sub('voicedelay\s+\d+', '', line).replace('::', ':')

with open(script_path, encoding='utf-8') as f:
    lines = f.readlines()

jp_voicedelay_to_insert = None
en_voicedelay_to_insert = None

insert_next_line = False

output_lines = []
lines_to_remove_voicedelay = {}

offset = 0
for line_no, line in enumerate(reversed(lines)):
    match = re.search('voicedelay[^#]*#', line)
    se_match = re.search('se\dv?\s+\d+', line)


    if insert_next_line:
        if se_match:
            pass
        else:
            # look for a se command on previous lines
            if en_voicedelay_to_insert is not None:
                print(f"{line} -> {en_voicedelay_to_insert}")
                offset += 1
                output_lines.append(f'langen:{en_voicedelay_to_insert}\n')
                en_voicedelay_to_insert = None


            if jp_voicedelay_to_insert is not None:
                print(f"{line} -> {jp_voicedelay_to_insert}")
                offset += 1
                output_lines.append(f'langjp:{jp_voicedelay_to_insert}\n')
                jp_voicedelay_to_insert = None

            insert_next_line = False

    if match:
        se_match = None

        print(line)

        if 'langen' in line:
            if en_voicedelay_to_insert is not None:
                raise Exception(f"Error - couldn't insert {en_voicedelay_to_insert}")
            en_voicedelay_to_insert = get_voicedelay(line)
            en_source_line_no = line_no
        else:
            if jp_voicedelay_to_insert is not None:
                raise Exception(f"Error - couldn't insert {jp_voicedelay_to_insert}")
            jp_voicedelay_to_insert = get_voicedelay(line)
            jp_source_line_no = line_no

        lines_to_remove_voicedelay[line_no + offset] = True
        # output_line = remove_voicedelay_and_tidy(line)
    elif se_match:
        insert_next_line = True

    output_lines.append(line)

output_lines_2 = []

for line_no, line in enumerate(output_lines):
    if line_no in lines_to_remove_voicedelay:
        fixed_line = remove_voicedelay_and_tidy(line)
        output_lines_2.append(fixed_line)
    else:
        output_lines_2.append(line)


with open(output_path, 'w', encoding='utf-8') as output:
    output.writelines(reversed(output_lines_2))
