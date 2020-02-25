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
    output_file_path = str(argv[2])
    delete_all_on_start()
    bpy.ops.import_scene.obj(filepath=input_file_path)
    global_obj = bpy.data.objects[0]
    bpy.context.view_layer.objects.active = global_obj
    original_face_count = len(global_obj.data.polygons)
    bpy.ops.object.modifier_add(type='DECIMATE')
    decimate_modifier = global_obj.modifiers[0]
    global_obj.modifiers[decimate_modifier.name].decimate_type = 'DISSOLVE'
    global_obj.modifiers[decimate_modifier.name].angle_limit = 0.0872665
    bpy.ops.object.modifier_apply(apply_as='DATA', modifier=decimate_modifier.name)
    bpy.ops.object.modifier_add(type='TRIANGULATE')
    triangulate_modifier = global_obj.modifiers[0]
    bpy.ops.object.modifier_apply(apply_as='DATA', modifier=triangulate_modifier.name)
    new_face_count = len(global_obj.data.polygons)
    print_out("Decimated mesh from " + str(original_face_count) + " to " + str(new_face_count) + " faces.")
    print_face_count(new_face_count)
    bpy.ops.export_scene.obj(filepath=output_file_path)
    print_out("Finished operation. Mesh can be found at " + output_file_path + ".")
    sys.exit(0)
              
main()