﻿using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    [Flags]
    public enum LayoutFlags
    {
        None = 0,

        AnimatedShow = 1<<1,
        AnimatedDismiss = 1<<2,


        AnimatedTransitions = AnimatedShow | AnimatedDismiss
    }




    /// <summary>
    /// ILayoutEngine implementations will use information you provide here to
    /// construct layouts. You can subclass this to provide additional information
    /// to custom layout implementations
    /// </summary>
    public class LayoutOptions
    {
        public LayoutFlags Flags = LayoutFlags.None; 

        // layout constraint controls
        public Func<Vector2f> PinSourcePoint2D;
        public Func<Vector2f> PinTargetPoint2D;

        // distance UI element is shifted in/out of standard UI plane/surface
        public float DepthShift = 0;
    }


    public interface ILayout
    {
        bool Contains(SceneUIElement element);
        void Add(SceneUIElement element, LayoutOptions options);
        void Remove(SceneUIElement element, bool bDestroy);

        // remove all UI elements
        void RemoveAll(bool bDestroy);


        /// <summary>
        /// Generally we want to set up a UI in some abstract coordinates, and then
        /// map to eg pixel coordinates. However currently we cannot handle that
        /// transparently, so client must do it themselves by multiplying by UIScaleFactor
        /// </summary>
        float UIScaleFactor { get; }


        /// <summary>
        /// If this layout has a "frame" that other things can be aligned relative to,
        /// you can access that frame via this property. May be null.
        /// </summary>
        IBoxModelElement BoxElement { get; }

    }
}
