lines_need_insertion = set()

with open("lines_need_sl_insertion.txt", 'r', encoding='utf-8') as f:
	for line in f:
		lines_need_insertion.add(int(line))

with open("../../InDevelopment/ManualUpdates/0.utf", 'r', encoding='utf-8') as file:
	all_lines = file.readlines()

output = []

# total_redundant_sl = 0
for i in range(len(all_lines)):
	line = all_lines[i]

	if i in lines_need_insertion:
		output.append('sl\n')

	output.append(line)


with open("lines_with_full_sl.u", 'w', encoding='utf-8') as f:
	f.writelines(output)
