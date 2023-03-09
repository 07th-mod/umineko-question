import glob
from pathlib import Path
import subprocess
import os

# Use this script for converting voice .at3 files from the console games
# You must have a newish FFMpeg installed to use this script
#
# Question arcs uses following settings
# Format: Vorbis in .ogg container
# Sample Rate: 44100hz
# Channels: 2 (Answer arcs uses 1. It shouldn't make any difference, but just to keep it consistent should use 2 for question arcs)

output_folder = 'output'
os.makedirs(output_folder, exist_ok=True)

for path in glob.glob('./input/*.at3'):
    path = Path(path)
    output_path = Path(output_folder).joinpath(Path(path.name).with_suffix('.ogg'))
    print(f'Converting {path}')
    subprocess.check_call(['ffmpeg', '-i', path, '-ar', '44100', '-ac', '2', output_path])
