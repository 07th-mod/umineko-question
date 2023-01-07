# This file is copied from https://github.com/drojf/umineko_python_scripts/tree/master/Fix_Automatic_Voice_Lines

#extracts aliases from file (assumes all aliases are quoted strings.
import re


class AliasMapping:
    aliasRegex = re.compile(r'''stralias\s+(\w+)\s*,\s*"([^"]*)"''')

    def __init__(self):
        self.aliasTable = {}

    def parse(self, line):
        match = self.aliasRegex.search(line)
        if match:
             self.aliasTable[match[1].lower()] = match[2]

    def get(self, alias):
        return self.aliasTable[alias.lower()]

    def contains(self, alias):
        return alias.lower() in self.aliasTable

    def __str__(self):
        return str(self.aliasTable)


def convertVoiceSecondsToScriptDelay(voiceLength):
    return int(round(voiceLength, 2)*1000)