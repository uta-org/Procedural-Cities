#!/bin/bash

files=$(find . -type f -name "*.gltf")
for f in $files; do
	file="${f/.\//}"
	file_fbx="${file/.gltf/.fbx}"
	# echo "$file"
	blender --background --python export_fbx.py -- "$file" "$file_fbx"
done

# blender --background --python export_fbx.py -- wardrobe.gltf wardrobe.fbx
