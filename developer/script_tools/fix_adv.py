

def has_japanese_characters(string):
    for c in string:
        if c == '…':
            return True
        if 0x3000 < ord(c) < 0x303f: # japanese punctuation
            return True
        if 0x3040 < ord(c) < 0x3094: # hiragana
            return True
        elif 0x30a0  < ord(c) < 0x30ff: # katakana
            return True
        elif 0x4e00   < ord(c) < 0x9faf: # ideographs
            return True

    return False


def line_is_langen_or_langjp(line):
    return line.strip().startswith('langjp') or line.strip().startswith('langen')


def line_has_english_text(line):
    if 'dwave_eng' in line or 'dwave_jp' in line:
        return True

    # if '^' in line and len(line) > 45:
    #     return True

    # this is a bit unsafe as it's possible for a line to have ^ but still display nothing...but checking through it seems this doesn't happen for the purposes of this script
    if '^' in line:
        return True

    return False


def main():

    sample_areas = []

    script_path = '0.utf'

    with open(script_path, encoding='utf-8') as f:
        lines = f.readlines()

    count = 0

    got_advchar = False
    advchar_line = None

    # Look for an advchar -1, followed by a langen/langjp line which doesn't look like a proper dialogue line
    # eg is something like langen\ or langen!sd
    for i, line in enumerate(lines):
        if line.strip() == 'advchar "-1"':
            got_advchar = True
            advchar_line = i


        if got_advchar and line_is_langen_or_langjp(line):
            if has_japanese_characters(line) or line_has_english_text(line):
                got_advchar = False
                advchar_line = None
            else:
                # sample_areas.append(advchar_line)
                print(f"{advchar_line+1}->{i+1}: {line.rstrip()}")
                count += 1

                got_advchar = False
                advchar_line = None

    got_any_advchar = False
    any_advchar_line = None

    print(f"Got {count} matches")

    print("-------------------------")

    print("Any advchar + noclear_cw")
    # Look for any advchar line, followed by a noclear_cw
    for i, line in enumerate(lines):

        if line.strip().startswith('advchar'):
            got_any_advchar = True
            any_advchar_line = i

        if got_any_advchar and line_is_langen_or_langjp(line):
            if 'noclear_cw' in line:
                # sample_areas.append(any_advchar_line)
                print(f"{any_advchar_line + 1}->{i + 1}: {line.rstrip()}")
                got_any_advchar = False
                any_advchar_line = None
            else:
                got_any_advchar = False
                any_advchar_line = None

    got_any_advchar = False
    any_advchar_line = None

    print("-------------------------")

    print("Any advchar -> Any advchar")
    # TODO: look for an advchar, followed by another advchar? like
    # ADVCHAR -1, advchar 10? (or any two advchars, doesn't have to be a -1 adv char)
    # with no langen in-between implies it has no effect
    # Need to ignore langen/langjp lines which are not 'real' lines, using same algorithm as previously
    # But would only make a difference on automatic lines I think.
    for i, line in enumerate(lines):
        if got_any_advchar:
            if line.strip().startswith('advchar'):
                sample_areas.append(any_advchar_line)
                print(f"{any_advchar_line + 1}->{i + 1}: {lines[any_advchar_line].rstrip()} -> {line.rstrip()}")

                for lines_to_print_i in range(any_advchar_line, i + 1):
                    print(f"\t{lines[lines_to_print_i].rstrip()}")

                ## Delete the redundant advchar line
                lines[any_advchar_line] = "\n"

                got_any_advchar = False
                any_advchar_line = None

            elif line_is_langen_or_langjp(line) and (has_japanese_characters(line) or line_has_english_text(line)):
                got_any_advchar = False
                any_advchar_line = None


        if line.strip().startswith('advchar'):
            got_any_advchar = True
            any_advchar_line = i

    ## Sampler Script Generator

    # firstRun = True
    # count = len(sample_areas)
    # for i in reversed(sample_areas):
    #     startLine = f'*drojf_test_{count}\n'
    #     endLine = f'goto *drojf_test_{count+1}\n'
    #     if firstRun:
    #         firstRun = False
    #         endLine = f'goto *drojf_test_{count}\n'
    #
    #     print(endLine)
    #     print(startLine)
    #     lines.insert(i + 20, endLine)
    #     lines.insert(i - 20, f'langen^Test {count}: line {i+1}^\\\n')
    #     lines.insert(i - 20, startLine)
    #     count -= 1
    #     # lines.insert(i, 'goto *')
    #
    # for i, line in enumerate(lines):
    #     if line.strip() == ';第一話　Legend of the golden witch　仮打ち':
    #         lines[i] = 'goto *drojf_test_1'



    with open('0.u_sampler', 'w', encoding='utf-8') as f:
        f.writelines(lines)

if __name__ == '__main__':
    main()