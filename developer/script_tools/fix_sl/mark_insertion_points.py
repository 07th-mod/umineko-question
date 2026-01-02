from typing import List


class Lexeme:
	def __init__(self, type, text):
		self.text = text
		self.type = type

	def __repr__(self):
		return f"{self.type}: {self.text}"


class Processor:

	def __init__(self):
		self.need_insertion = []
		self.queued_sl = False

	def process_map_line(self, i: int,  line: str, map_line: List[Lexeme]):
		if len(map_line) == 0 or map_line[0].text == 'langjp':
			return None

		for lexeme in map_line:
			if lexeme.type == 'WORD' and lexeme.text == 'langen':
				if self.queued_sl:
					self.need_insertion.append(i)
					self.queued_sl = False

		if len(map_line) != 0 and map_line[0].type == 'WORD' and map_line[0].text == 'langen':
			final_token = map_line[-1]
			if not final_token.type == 'BACK_SLASH' \
					and not final_token.type == 'AT_SYMBOL' \
					and not final_token.text == 'noclear_cw' \
					and not final_token.text == 'voicedelay' \
					and not final_token.text == '!s1' \
					and not final_token.text == '!sd' \
					and not final_token.text == '!w500' \
					and not final_token.text == '!d1000'\
					and not '~c4~         You may perform witchcraft' in final_token.text\
					and not '~c4~          to jump forward in time.' in final_token.text:
				if final_token.type != 'OPERATOR':
					print("Final token is", final_token.type, "at line", i+1, line.rstrip())
				self.queued_sl = True


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

	with open("lines_need_sl_insertion.txt", 'w', encoding='utf-8') as f:
		f.writelines(str(i) + '\n' for i in p.need_insertion)

