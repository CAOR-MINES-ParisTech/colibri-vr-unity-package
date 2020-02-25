### Copyright 2019-2020 MINES ParisTech (PSL University)
### This work is licensed under the terms of the MIT license, see the LICENSE file.
### 
### Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

import sys, os
sys.path.append(sys.argv[sys.argv.index("--") + 1:][0])
import bpy
from Blender_Core import print_out, print_err, delete_all_on_start

def main() :
    argv = sys.argv[sys.argv.index("--") + 1:]
    input_file_path = str(argv[1])
    output_file_path = str(argv[2])
    delete_all_on_start()
    print_out("Importing mesh from " + input_file_path + ".")
    bpy.ops.import_scene.obj(filepath=input_file_path)
    obj = bpy.data.objects[0]
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    print_out("Applying the Smart UV Project algorithm.")
    bpy.ops.uv.smart_project()
    bpy.ops.object.mode_set(mode='OBJECT')
    print_out("Exporting mesh to " + output_file_path + ".")
    bpy.ops.export_scene.obj(filepath=output_file_path)
    sys.exit(0)
              
main()