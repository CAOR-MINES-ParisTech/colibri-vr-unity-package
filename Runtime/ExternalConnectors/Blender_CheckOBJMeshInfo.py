### Copyright 2019-2020 MINES ParisTech (PSL University)
### This work is licensed under the terms of the MIT license, see the LICENSE file.
### 
### Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

import sys, os
sys.path.append(sys.argv[sys.argv.index("--") + 1:][0])
import bpy
from Blender_Core import print_out, print_err, delete_all_on_start, print_face_count

def main() :
    argv = sys.argv[sys.argv.index("--") + 1:]
    input_file_path = str(argv[1])
    delete_all_on_start()
    bpy.ops.import_scene.obj(filepath=input_file_path)
    face_count = len(bpy.data.objects[0].data.polygons)
    print_face_count(face_count)
    print_out("Mesh currently has " + str(face_count) + " faces.")
    sys.exit(0)
              
main()