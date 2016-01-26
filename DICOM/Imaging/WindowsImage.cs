﻿// Copyright (c) 2012-2016 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Dicom.Imaging
{
    using System.Collections.Generic;
    using System.IO;

    using Windows.UI.Xaml.Media.Imaging;

    using Dicom.Imaging.Render;
    using Dicom.IO;

    /// <summary>
    /// Convenience class for non-generic access to <see cref="WindowsImage"/> image objects.
    /// </summary>
    public static class WindowsImageExtensions
    {
        /// <summary>
        /// Convenience method to access UWP <see cref="IImage"/> instance as UWP <see cref="WriteableBitmap"/>.
        /// The returned <see cref="WriteableBitmap"/> will be disposed when the <see cref="IImage"/> is disposed.
        /// </summary>
        /// <param name="image"><see cref="IImage"/> object.</param>
        /// <returns><see cref="WriteableBitmap"/> contents of <paramref name="image"/>.</returns>
        public static WriteableBitmap AsWriteableBitmap(this IImage image)
        {
            return image.As<WriteableBitmap>();
        }
    }

    /// <summary>
    /// <see cref="IImage"/> implementation of a Universal Windows Platform <see cref="WriteableBitmap"/>.
    /// </summary>
    public sealed class WindowsImage : ImageBase<WriteableBitmap>
    {
        #region CONSTRUCTORS

        /// <summary>
        /// Initializes an instance of the <see cref="WindowsImage"/> object.
        /// </summary>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        public WindowsImage(int width, int height)
            : base(width, height, new PinnedIntArray(width * height), null)
        {
        }

        /// <summary>
        /// Initializes an instance of the <see cref="WindowsImage"/> object.
        /// </summary>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="pixels">Array of pixels.</param>
        /// <param name="image">Writeable bitmap image.</param>
        private WindowsImage(int width, int height, PinnedIntArray pixels, WriteableBitmap image)
            : base(width, height, pixels, image)
        {
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Renders the image given the specified parameters.
        /// </summary>
        /// <param name="components">Number of components.</param>
        /// <param name="flipX">Flip image in X direction?</param>
        /// <param name="flipY">Flip image in Y direction?</param>
        /// <param name="rotation">Image rotation.</param>
        public override void Render(int components, bool flipX, bool flipY, int rotation)
        {
            var w = this.width;
            var h = this.height;

            var bytes = ToBytes(ref w, ref h, components, flipX, flipY, rotation, this.pixels.Data);
            this.image = Create(w, h, bytes);
        }

        /// <summary>
        /// Draw graphics onto existing image.
        /// </summary>
        /// <param name="graphics">Graphics to draw.</param>
        public override void DrawGraphics(IEnumerable<IGraphic> graphics)
        {
            var dstStride = 4 * this.image.PixelWidth;

            using (var stream = this.image.PixelBuffer.AsStream())
            {
                foreach (var graphic in graphics)
                {
                    var layer = graphic.RenderImage(null).As<WriteableBitmap>();
                    var srcStride = 4 * graphic.ScaledWidth;

                    var bytes = layer.PixelBuffer.ToArray();

                    int srcOffs = 0, dstOffs = dstStride * graphic.ScaledOffsetY + 4 * graphic.ScaledOffsetX;
                    for (var j = 0; j < graphic.ScaledHeight; ++j, srcOffs += srcStride, dstOffs += dstStride)
                    {
                        stream.Seek(dstOffs, SeekOrigin.Begin);
                        stream.Write(bytes, srcOffs, graphic.ScaledWidth);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a deep copy of the image.
        /// </summary>
        /// <returns>Deep copy of this image.</returns>
        public override IImage Clone()
        {
            return new WindowsImage(
                this.width,
                this.height,
                new PinnedIntArray(this.pixels.Data),
                this.image == null ? null : Create(this.image));
        }

        private static byte[] ToBytes(
            ref int width,
            ref int height,
            int components,
            bool flipX,
            bool flipY,
            int rotation,
            int[] data)
        {
            var processed = Rotate(ref width, ref height, rotation, data);
            processed = Flip(width, height, flipX, flipY, processed);

            // TODO Consider to make use of "components"
            var length = 4 * width * height;
            var bytes = new byte[length];

            Buffer.BlockCopy(processed, 0, bytes, 0, length);
            return bytes;
        }

        private static int[] Rotate(ref int width, ref int height, int angle, int[] data)
        {
            int[] result;
            angle %= 360;

            var i = 0;
            if (angle > 0 && angle <= 90)
            {
                result = new int[width * height];
                for (var x = 0; x < width; x++)
                {
                    for (var y = height - 1; y >= 0; y--, i++)
                    {
                        result[i] = data[y * width + x];
                    }
                }
                var tmp = width;
                width = height;
                height = tmp;
            }
            else if (angle > 90 && angle <= 180)
            {
                result = new int[width * height];
                for (var y = height - 1; y >= 0; y--)
                {
                    for (var x = width - 1; x >= 0; x--, i++)
                    {
                        result[i] = data[y * width + x];
                    }
                }
            }
            else if (angle > 180 && angle <= 270)
            {
                result = new int[width * height];
                for (var x = width - 1; x >= 0; x--)
                {
                    for (var y = 0; y < height; y++, i++)
                    {
                        result[i] = data[y * width + x];
                    }
                }
                var tmp = width;
                width = height;
                height = tmp;
            }
            else
            {
                result = data;
            }
            return result;
        }

        private static int[] Flip(int w, int h, bool flipX, bool flipY, int[] p)
        {
            var i = 0;
            int[] tmp, result;

            if (flipX)
            {
                tmp = new int[w * h];
                for (var y = h - 1; y >= 0; y--)
                {
                    for (var x = 0; x < w; x++, i++)
                    {
                        tmp[i] = p[y * w + x];
                    }
                }
            }
            else
            {
                tmp = p;
            }
            if (flipY)
            {
                result = new int[w * h];
                for (var y = 0; y < h; y++)
                {
                    for (var x = w - 1; x >= 0; x--, i++)
                    {
                        result[i] = tmp[y * w + x];
                    }
                }
            }
            else
            {
                result = tmp;
            }

            return result;
        }

        private static WriteableBitmap Create(int width, int height, byte[] bytes)
        {
            var bitmap = new WriteableBitmap(width, height);
            bytes.CopyTo(bitmap.PixelBuffer);
            bitmap.Invalidate();
            return bitmap;
        }

        private static WriteableBitmap Create(WriteableBitmap image)
        {
            var bitmap = new WriteableBitmap(image.PixelWidth, image.PixelHeight);
            image.PixelBuffer.CopyTo(bitmap.PixelBuffer);
            bitmap.Invalidate();
            return bitmap;
        }

        #endregion
    }
}