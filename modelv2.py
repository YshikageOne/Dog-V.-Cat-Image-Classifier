#Version 2 (85% accuracy)
#without any pretrain models or optimizations except for ADAM


import os
os.environ['TF_ENABLE_ONEDNN_OPTS'] = '0'

import tensorflow as tf
from keras.src.legacy.preprocessing.image import ImageDataGenerator
from keras import Sequential
from keras.src.layers import CenterCrop, Conv2D, MaxPooling2D, Flatten, Dense
from PIL import Image
import shutil

#import the dataset
dataset = r'C:\Users\ausus\Desktop\Coding Stuff\Python\SchoolStuff\Machine Learning\Dog v. Cat\Dataset'

#setted image dimensions
image_width, image_height = 150,150


#checks for valid image files
def valid_image(file_path):
    try:
        Image.open(file_path).verify() #to check if the file is an image
        return True
    except (IOError, SyntaxError) as e:
        with open("error_log.txt", "a") as error_file:
            error_file.write(f"Skipping corrupted file: {file_path} - {e}\n")
        corrupted_folder = "corrupted_images"  #create a folder for corrupted images
        os.makedirs(corrupted_folder, exist_ok=True)
        if os.path.exists(file_path):  #check if the file exists before moving
            shutil.move(file_path, os.path.join(corrupted_folder, os.path.basename(file_path)))
        return False

def filter_valid_image(generator):
    for x, y in generator:
        valid_indices = []
        for i, image_path in enumerate(generator.filenames):
            full_path = os.path.join(generator.directory, image_path)
            if os.path.exists(full_path) and valid_image(full_path):
                valid_indices.append(i)

        filtered_indices = []
        for i in valid_indices:
            if i < len(x):
                filtered_indices.append(i)

        if filtered_indices:
            yield x[filtered_indices], y[filtered_indices]

#image data generation for data augmentation
datagen = ImageDataGenerator(
    rescale = 1./255,  #splits the 0-255 value of grayscale and rgb to 0-1
    shear_range = 0.2,
    zoom_range = 0.2,
    horizontal_flip = True,
    validation_split = 0.2, #diving the dataset 80/20 ratio 20% for testing/validation

    #to crop the images to match the setted dimensions for consistency purposes
    preprocessing_function = CenterCrop(height = image_height, width = image_width)
)

#training data
train_generator = datagen.flow_from_directory(
    dataset,
    target_size = (image_width, image_height),
    batch_size = 32,
    class_mode = 'binary',
    subset = 'training'
)

#testing/validation data
validation_generator = datagen.flow_from_directory(
    dataset,
    target_size = (image_width, image_height),
    batch_size = 32,
    class_mode = 'binary',
    subset = 'validation'
)

#making the CNN model (Convolution Neural Network)
model = Sequential([
    Conv2D(32, (3, 3), activation='relu', input_shape=(image_width, image_height, 3)),
    MaxPooling2D((2, 2)),
    Conv2D(64, (3, 3), activation='relu'),
    MaxPooling2D((2, 2)),
    Flatten(),
    Dense(128, activation='relu'),
    Dense(1, activation='sigmoid')  #binary classification (cat or dog)
])

#compiling the model
model.compile(
    optimizer = 'adam',
    loss = 'binary_crossentropy',
    metrics = ['accuracy']
)

#train
model.fit(
    filter_valid_image(train_generator),
    steps_per_epoch = train_generator.samples // train_generator.batch_size,
    epochs = 10,
    validation_data=filter_valid_image(validation_generator),
    validation_steps = validation_generator.samples // validation_generator.batch_size,

)

model.save(r'C:\Users\ausus\Desktop\Coding Stuff\Python\SchoolStuff\Machine Learning\Dog v. Cat\version2.h5')

#evaluate the model on test data
loss, accuracy = model.evaluate(validation_generator, steps=validation_generator.samples // validation_generator.batch_size)
print('Test accuracy:', accuracy)


