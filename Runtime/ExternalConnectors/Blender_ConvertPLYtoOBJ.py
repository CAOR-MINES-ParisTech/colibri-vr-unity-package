### Copyright 2019-2020 MINES ParisTech (PSL University)
### This work is licensed under the terms of the MIT license, see the LICENSE file.
### 
### Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

import sys, os
sys.path.append(sys.argv[sys.argv.index("--") + 1:][0])
import bpy
from math import radians
from Blender_Core import print_out, print_err, delete_all_on_start, print_face_count

def main() :
    argv = sys.argv[sys.argv.index("--") + 1:]
    input_file_path = str(argv[1])
    output_file_path = str(argv[2])
    delete_all_on_start()
    print_out("Importing mesh from " + input_file_path + ".")
    bpy.ops.import_mesh.ply(filepath=input_file_path)
    global_obj = bpy.data.objects[0]
    face_count = len(global_obj.data.polygons)
    print_face_count(face_count)
    print_out("Rotating mesh.")
    global_obj.rotation_euler[0] = radians(-90)
    global_obj.rotation_euler[2] = radians(180)
    print_out("Exporting mesh to " + output_file_path + ".")
    bpy.ops.export_scene.obj(filepath=output_file_path)
    print_out("Finished operation. Mesh can be found at " + output_file_path + ".")
    sys.exit(0)
              
main()