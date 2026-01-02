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

num_channels = '2'

output_folder = 'output'
os.makedirs(output_folder, exist_ok=True)

for path in glob.glob('./input/*.at3'):
    path = Path(path)
    temp = Path(output_folder).joinpath(Path(path.name).with_suffix('.wav'))

    # The length will be incorrect for certain .at3 files if converting directly to .ogg
    # To get around this, convert to .wav first, then conver the .wav to .ogg
    print(f'Converting {path}')
    subprocess.check_call(['ffmpeg', '-i', path, '-ar', '44100', '-ac', num_channels, temp])

    dst = Path(temp).with_suffix('.ogg')

    subprocess.check_call(['ffmpeg', '-y', '-i', temp, '-ar', '44100', '-ac', num_channels, '-q', '8', dst])

    os.remove(temp)