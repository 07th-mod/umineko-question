import csv
import os
import shutil

from PIL import Image

input_ps3_folder = r'C:\umineko stuff\nocturne ps3 backgrounds'  #ps3 image backgrounds
input_original_folder = r'C:\umineko stuff\arc.nsa original extracted\bmp\background' #original steam backgrounds

output_folder = r'c:\temp\output'

OUTPUT_RES = (1707, 960)

def get_filename_no_ext(file_path):
    baseName = os.path.basename(file_path)                          #get filename with extension
    file_name_no_ext, file_extension = os.path.splitext(baseName)   #get filename without extension. Extension includes the "."
    return file_name_no_ext

def file_exists(file_path):
    try:
        with open(file_path, "r") as f:
            return True
    except:
        return False

def get_image_with_bg(folder_path, filename):
    file_name_no_ext, file_extension = os.path.splitext(filename)

    image_path = os.path.join(folder_path, filename)
    transparency_path = os.path.join(folder_path, file_name_no_ext + "_bg" + file_extension)

    if file_exists(transparency_path):
        print("-> Also found " + transparency_path, end="")
        back_image = Image.open(transparency_path)
        top_image = Image.open(image_path)
        return Image.alpha_composite(back_image, top_image)          #overlay top image on background image
    else:
        return Image.open(image_path)

##### IMAGE MANIPULATION FUNCTIOSN #####
#note-input images must not have any transparency!!
def resize_and_add_image_right(original_image, right_image):
    left_image = original_image.resize(OUTPUT_RES)
    right_image_resized = right_image.resize(OUTPUT_RES)
    #right_image = Image.new('RGBA', new_res)
    whole_image = Image.new('RGB', (OUTPUT_RES[0]*2, OUTPUT_RES[1]))
    whole_image.paste(left_image, box=(0,0))
    whole_image.paste(right_image_resized, box=(OUTPUT_RES[0], 0))
    whole_image = whole_image.convert('RGB')
    return whole_image

def resize_and_add_black_top(im):
    bottom_image = im.resize(OUTPUT_RES)
    #top_image = Image.new('RGBA', OUTPUT_RES)
    whole_image = Image.new('RGB', (OUTPUT_RES[0], OUTPUT_RES[1]*2))
    #whole_image.paste(top_image, box=(0,0))
    whole_image.paste(bottom_image, box=(0, OUTPUT_RES[1]))
    return whole_image

#turns transparency
def make_mask_from_image(im):
    (_,_,_,alpha_channel) = im.split()
    return alpha_channel.point([255-x for x in range(256)]) #invert the image

def make_gradient_image_door():
    width = 1000

    gradient = [2*(x/(width-1)-.5) for x in range(width)]   #-1 to +1 across an array of arbitrary length

    cutoff = .3
    offset = .3
    gradient = [(x-offset) / cutoff for x in gradient]       #now the center 30% will be in the range -1, 1
    gradient = [(x+1)*255 for x in gradient]                #map the range from [-1,1] to [0,255]
    gradient = [round(max(min(x,255),0)) for x in gradient] #clamp resultant values and make integer

    #print(gradient)
    im = Image.frombytes('L', (width,1), bytes(gradient))
    return im.resize(OUTPUT_RES, resample=Image.LINEAR).transpose(Image.FLIP_LEFT_RIGHT)

################### METHODS START ##############
def normal(**kwargs):
    im = get_image_with_bg(input_ps3_folder, kwargs['ps3_path'])
    return im.resize(OUTPUT_RES, resample=Image.LANCZOS)

def use_ps3_same_name(**kwargs):
    filename = get_filename_no_ext(kwargs['original_path']) + '.png'
    return normal(ps3_path=filename)

def make_greyscale(**kwargs):
    return normal(ps3_path=kwargs['manual_path']).convert('L') #convert to greyscale

def use_alternate(**kwargs):
    return normal(ps3_path=kwargs['manual_path'])

#def left_image_right_black
#must specify which image to use for this!
def top_black_bottom_image(**kwargs):
    im = get_image_with_bg(input_ps3_folder, kwargs['manual_path'])
    return resize_and_add_black_top(im)

def left_image_right_door_gradient(**kwargs):
    im = get_image_with_bg(input_ps3_folder, kwargs['manual_path'])
    return resize_and_add_image_right(im, make_gradient_image_door())

def left_image_right_masked_image(**kwargs):
    im = get_image_with_bg(input_ps3_folder, kwargs['manual_path'])
    #need to remove alpha parts from the image if it has one, by compositing the image against black
    black = Image.new('RGBA', im.size, color=(0,0,0,255))
    im_on_black_background = Image.alpha_composite(black, im)
    return resize_and_add_image_right(im_on_black_background, make_mask_from_image(im))

#note: gradient image should have about 135/640 occupied by white->black. left side is white, right side is black.

def process_tall_image(im):
    black = Image.new('RGBA', im.size, color=(0,0,0,255))
    im_on_black_background = Image.alpha_composite(black, im)
    scaling_factor = OUTPUT_RES[0] / im.width
    output_height = round(im.height * scaling_factor)
    #im_on_black_background.thumbnail((OUTPUT_RES[0], 9999999999))       #resize such that width is 1707, but height can be whatever
    return im_on_black_background.resize((OUTPUT_RES[0],output_height), resample=Image.LANCZOS)

def tall_image(**kwargs):
    filename = None
    if manual_path != '':
        filename = kwargs['manual_path']
    else:
        filename = get_filename_no_ext(kwargs['original_path']) + '.png'

    im = get_image_with_bg(input_ps3_folder, filename)
    return process_tall_image(im)

############ ORIGINAL IMAGE Methods #########
# Note: any effect files should retain their original height so the game processes them correctly!
def get_widescreen_width(height):
    return round(height * 16 / 9)

def get_double_widescreen_width(height):
    return round(height * 16 * 2 / 9)

def tall_image_original(**kwargs):
    filename = os.path.join(input_original_folder, kwargs['original_path'])
    im = Image.open(filename).convert(mode='RGBA')
    return process_tall_image(im)

def stretch_original_double_width(**kwargs):
    full_path = os.path.join(input_original_folder, kwargs['original_path'])
    im = Image.open(full_path)
    return im.resize((get_double_widescreen_width(im.height),im.height), resample=Image.LANCZOS)

#NOTE: this will stretch the original images, but in most cases it doesn't look that bad surprisingly
def stretch_original(**kwargs):
    full_path = os.path.join(input_original_folder, kwargs['original_path'])
    im = Image.open(full_path)
    return im.resize((get_widescreen_width(im.height), im.height), resample=Image.LANCZOS)

#input is original game's asset, will put it in center of screen and black bars either side
def stretch_height_center_screen(**kwargs):
    filename = os.path.join(input_original_folder, kwargs['original_path'])
    im = Image.open(filename)
    new_width = get_widescreen_width(im.height)
    x_offset = round((new_width -im.width)/2)

    black = Image.new('RGBA', (new_width, im.height), color=(0,0,0,255))
    black.paste(im, (x_offset, 0))
    return black


method_lookup_table = {'': normal,
                       'make_greyscale':make_greyscale,
                       'tall_image': tall_image,
                       'tall_image_original':tall_image_original,
                       'stretch_original': stretch_original,
                       'stretch_original_double_width': stretch_original_double_width,
                       'use_alternate': use_alternate,
                       'top_black_bottom_image': top_black_bottom_image,
                       'left_image_right_door_gradient': left_image_right_door_gradient,
                       'left_image_right_masked_image': left_image_right_masked_image,
                       'use_ps3_same_name':use_ps3_same_name,
                       'stretch_height_center_screen':stretch_height_center_screen}

with open ('database.csv', newline='') as csvfile:
    reader = csv.reader(csvfile, delimiter=',')
    for row in reader:
        original_path = row[0].strip()
        ps3_path = row[1].strip()
        method = row[2].strip()
        manual_path = row[3].strip()
        similarity = row[4].strip()

        name, ext = os.path.splitext(original_path)

        if method == 'english_asset':
            save_path = os.path.join(output_folder, original_path)
        else:
            save_path = os.path.join(output_folder, name + '.png')

        if file_exists(save_path):
            poke_image = Image.open(save_path)
            _, save_ext = os.path.splitext(save_path)
            print('{},{},{},{},{}'.format(original_path, save_ext, poke_image.width, poke_image.height, method))
            continue

        if method == 'no_modification':
            original_image = Image.open(os.path.join(input_original_folder, original_path))
            original_image.save(save_path)
            continue

        if method == 'english_asset':
            shutil.copy(os.path.join(input_original_folder, original_path), save_path)
            continue

        if method == 'not_used':
            print('Skipping', name, 'as it is never used in the script!')
            continue

        if method in method_lookup_table:
            print("Using method [{}] for [{}]".format(method, original_path))
            processed_image = method_lookup_table[method](ps3_path=ps3_path, manual_path=manual_path, original_path=original_path)

            try:
                containing_folder_path, _ = os.path.split(save_path)
                os.makedirs(containing_folder_path)
            except:
                pass

            processed_image.save(save_path)
        else:
            print("ERROR: No method matches [{}] for [{}]".format(method, name))
