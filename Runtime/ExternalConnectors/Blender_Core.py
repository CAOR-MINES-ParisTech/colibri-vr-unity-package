### Copyright 2019-2020 MINES ParisTech (PSL University)
### This work is licensed under the terms of the MIT license, see the LICENSE file.
### 
### Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

import sys
import bpy

def print_out(lineToWrite) :
    sys.stdout.write(lineToWrite + "\n")
    sys.stdout.flush()

def print_err(errorLine) :
    sys.stderr.write(errorLine + "\n")
    sys.stderr.flush()

def delete_all_on_start() :
    if (len(bpy.data.objects) > 0) :
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.object.delete(use_global=False)

def print_face_count(face_count) :
    print_out("FACE_COUNT_OUTPUT:" + str(face_count))