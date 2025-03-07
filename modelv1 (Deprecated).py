import os
os.environ['TF_ENABLE_ONEDNN_OPTS'] = '0'

import tensorflow as tf
import numpy as np
from matplotlib import pyplot as plt
from keras.src.legacy.preprocessing.image import ImageDataGenerator
from tensorflow.python.keras import Sequential
from tensorflow.python.keras.layers import Conv2D, MaxPooling2D, Flatten, Dense, Dropout
from keras.src.optimizers import Adam

#import dataset
dataset = r'C:\Users\ausus\Desktop\Coding Stuff\Python\SchoolStuff\Machine Learning\Dog v. Cat\Dataset'
train_dir = os.path.join(dataset, 'train')
validation_dir = os.path.join(dataset, 'validation')

#for training and testing
datagen = ImageDataGenerator(
    rescale = 1./255, #splits the 0-255 value of grayscale and rgb to 0-1
    validation_split = 0.2 #20% for testing
)

#setting up the image data generator
train_generator = datagen.flow_from_directory(
    dataset,
    target_size = (150, 150),
    batch_size = 32,
    class_mode = 'binary',
    subset = 'training'
)

validation_generator = datagen.flow_from_directory(
    dataset,
    target_size=(150, 150),
    batch_size=32,
    class_mode='binary',
    subset='validation'
)

#Building the Model
model = Sequential([

    Conv2D(32, (3, 3), activation='relu', input_shape=(150, 150, 3)),
    MaxPooling2D(2, 2),
    Conv2D(64, (3, 3), activation='relu'),
    MaxPooling2D(2, 2),
    Conv2D(128, (3, 3), activation='relu'),
    MaxPooling2D(2, 2),

    Flatten(),
    Dense(512, activation='relu'),

    Dropout(0.5),
    Dense(1, activation='sigmoid')
])

#Compiling the Model
model.compile(optimizer=Adam(learning_rate=0.001),
              loss='binary_crossentropy',
              metrics=['accuracy'])

#Train the model
history = model.fit(
    train_generator,
    steps_per_epoch = train_generator.samples // train_generator.batch_size,
    validation_data = validation_generator,
    validation_steps = validation_generator.samples // validation_generator.batch_size,
    epochs = 15
)

#Evalutate the model
acc = history.history['accuracy']
val_acc = history.history['val_accuracy']
loss = history.history['loss']
val_loss = history.history['val_loss']

epochs_range = range(15)

plt.figure(figsize=(8, 8))
plt.subplot(1, 2, 1)
plt.plot(epochs_range, acc, label='Training Accuracy')
plt.plot(epochs_range, val_acc, label='Validation Accuracy')
plt.legend(loc='lower right')
plt.title('Training and Validation Accuracy')

plt.subplot(1, 2, 2)
plt.plot(epochs_range, loss, label='Training Loss')
plt.plot(epochs_range, val_loss, label='Validation Loss')
plt.legend(loc='upper right')
plt.title('Training and Validation Loss')
plt.show()
