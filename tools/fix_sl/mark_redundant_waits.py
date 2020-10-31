from typing import List
import re

class Lexeme:
	def __init__(self, type, text):
		self.text = text
		self.type = type

	def __repr__(self):
		return f"{self.type}: {self.text}"


def getTrueStringLength(s):
	s = s.replace('^', '').replace('!sd', '')
	s = re.sub('!d\d+', '', s)
	s = re.sub('!s\d+', '', s)
	s = re.sub('!w\d+', '', s)

	return len(s)


class Processor:

	def __init__(self):
		self.cumulative_text = []
		self.zero_lines = []
		self.short_lines = []
		self.last_was_sl = False
		self.last_sl_index = None

		self.sl_waiting_for_langen = False
		self.sl_waiting_for_langen_line_no = None
		self.sl_needs_move_down_lines = []
		self.sl_needs_replacement = None

	def process_map_line(self, i: int,  line: str, map_line: List[Lexeme]):
		# Skip lines at end of script
		if i > 301550:
			return None

		if len(map_line) == 0 or map_line[0].text == 'langjp':
			return None

		for lexeme in map_line:
			if lexeme.type == 'WORD' and lexeme.text == 'sl':
				self.sl_waiting_for_langen = True
				self.sl_waiting_for_langen_line_no = i
			elif lexeme.type == 'WORD' and lexeme.text == 'langen':
				self.sl_waiting_for_langen = False
				if self.sl_needs_replacement is not None:
					self.sl_needs_move_down_lines.append(f'{self.sl_needs_replacement}>{i}')

				self.sl_needs_replacement = None
			elif lexeme.type == 'WORD' and lexeme.text == 'langjp':
				pass
			elif self.sl_waiting_for_langen:
				print(f"Warning: sl at {self.sl_waiting_for_langen_line_no + 1} is in the wrong location, should be moved down")
				self.sl_waiting_for_langen = False
				self.sl_needs_replacement = self.sl_waiting_for_langen_line_no

			if lexeme.type == 'WORD' and lexeme.text == 'sl':
				self.handle_cumulative_text(i, line, current_is_sl=True)
				self.last_was_sl = True
				self.last_sl_index = i
			elif lexeme.type == 'AT_SYMBOL' or lexeme.type == 'BACK_SLASH':
				self.handle_cumulative_text(i, line, current_is_sl=False)
				self.last_was_sl = False
			elif lexeme.type == 'DIALOGUE':
				self.cumulative_text.append(lexeme.text)

		# TODO: if found excessively short line, print it out.

	def handle_cumulative_text(self, i, line, current_is_sl):
		length = getTrueStringLength(''.join(self.cumulative_text))
		if length < 10:
			if length == 0:
				if self.last_was_sl and not current_is_sl:
					print(f"Warning: last sl line at {self.last_sl_index + 1} is probably zero")
					self.zero_lines.append(self.last_sl_index)
				else:
					self.zero_lines.append(i)
			else:
				self.short_lines.append(i)
			print(f"Got {length} characters between clickwait: {self.cumulative_text} at {i + 1}: {line.rstrip()}")

		self.cumulative_text = []
		return length


if __name__ == '__main__':
	with open("decoded.txt", 'r', encoding='utf-8') as map:
		map_temp = map.readlines()

	map_lines = []
	for line in map_temp:
		pairs = []
		parts = line.split("\x00")
		for i in range(len(parts)//2):
			pairs.append(Lexeme(parts[2*i], parts[2*i+1]))

		map_lines.append(pairs)

	p = Processor()

	for i in range(len(map_lines)):
		p.process_map_line(i, map_temp[i], map_lines[i])

	with open("zero_lines.txt", 'w', encoding='utf-8') as f:
		f.writelines(str(i) + '\n' for i in p.zero_lines)

	with open("short_lines.txt", 'w', encoding='utf-8') as f:
		f.writelines(str(i) + '\n' for i in p.short_lines)

	with open("sl_needs_move_down.txt", 'w', encoding='utf-8') as f:
		f.writelines(str(i) + '\n' for i in p.sl_needs_move_down_lines)
