is_zero_lookup = set()
need_move_down = {}

with open("zero_lines.txt", 'r', encoding='utf-8') as f:
	for line in f:
		is_zero_lookup.add(int(line))


with open("sl_needs_move_down.txt", 'r', encoding='utf-8') as f:
	for line in f:
		splitLine = line.split('>')
		need_move_down[int(splitLine[0])] = int(splitLine[1])


with open("../../InDevelopment/ManualUpdates/0.utf", 'r', encoding='utf-8') as file:
	all_lines = file.readlines()

output = []

line_content_to_move_down = None
line_number_to_insert_above = None

# total_redundant_sl = 0
for i in range(len(all_lines)):
	line = all_lines[i]

	if line_content_to_move_down is not None and i == line_number_to_insert_above:
		output.append(line_content_to_move_down)
		line_content_to_move_down = None
		line_number_to_insert_above = None

	if line.strip() != 'sl':
		output.append(line)
	elif i in is_zero_lookup:
		# if line.strip() == 'sl':
		# 	total_redundant_sl += 1
		# else:
		# 	print(f"Non-sl Line {i + 1} is redundant: {line}")
		print(f"Will remove {i+1}: {line.strip()}")
	elif i in need_move_down:
		target = need_move_down[i]
		print(f"Will move {i+1} immediately above {target + 1}: {line.strip()}")
		line_content_to_move_down = line
		line_number_to_insert_above = target
	else:
		output.append(line)


with open("final_output.u", 'w', encoding='utf-8') as f:
	f.writelines(output)
