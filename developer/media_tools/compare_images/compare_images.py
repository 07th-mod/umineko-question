import glob
import os
import sys

from PIL import Image

#NOTE: this script assumes the left folder has more images than the right folder
# if this is not the case, some images will nott be shown from the right folder

left_folder = r'C:\temp\only_used_bg\bg' #r'C:\games\Steam\steamapps\common\Umineko Chiru\bg'
right_folder = r'c:\temp\ps3_bg_output'
output_dir = r'C:\temp\compare_bgs'

# if len(sys.argv) < 4:
# 	print("Usage: python compare_images.py leftFolder rightFolder output_folder")
# 	exit(-1)
#
# left_folder = sys.argv[1]
# right_folder = sys.argv[2]
# output_dir = sys.argv[3]

if not os.path.isdir(left_folder):
	print("Left folder doesn't exist or is not a directory")
	exit(-1)

if not os.path.isdir(right_folder):
	print("Right folder doesn't exist or is not a directory")
	exit(-1)

for leftImagePath in glob.iglob(os.path.join(left_folder, '**/*'), recursive=True):
	if os.path.isfile(leftImagePath):
		relativeImagePath = os.path.relpath(leftImagePath, left_folder)
		rightImagePath = os.path.join(right_folder, relativeImagePath)

		leftImage = Image.open(leftImagePath)

		if os.path.isfile(rightImagePath):
			rightImage = Image.open(rightImagePath)
			canvasWidth = leftImage.width + rightImage.width
			canvasHeight = max(leftImage.height, rightImage.height)
			canvas = Image.new(leftImage.mode, (canvasWidth, canvasHeight))
			canvas.paste(leftImage)
			canvas.paste(rightImage, (leftImage.width, 0))
		else:
			print("WARNING mod file doesn't exist at ", rightImagePath)
			canvas = leftImage

		canvas.thumbnail((800, 800))
		# canvas.show()

		output_path = os.path.join(output_dir, relativeImagePath)
		# ensure containing folder exists
		os.makedirs(os.path.dirname(output_path), exist_ok=True)

		print(output_path)
		canvas.save(output_path, compress_level=0)