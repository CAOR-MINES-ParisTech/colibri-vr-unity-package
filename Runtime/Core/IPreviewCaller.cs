/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine.Events;

namespace COLIBRIVR
{

    /// <summary>
    /// Interface for objects that call the preview window to display preview images.
    /// </summary>
    public interface IPreviewCaller
    {

#region INHERITANCE_PROPERTIES

        int previewIndex { get; set; }
        UnityEvent onPreviewIndexChangeEvent { get; }

#endregion //INHERITANCE_PROPERTIES

    }

}


