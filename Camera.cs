﻿using System.Numerics;

namespace SyntheseImage
{
    public class Camera
    {
        public Vector3 origine;
        public int height, width;
        public Vector3 direction;
        public Vector3 focus;
        private float distanceFocus;

        public Camera(Vector3 _origine, int _width, int _height, Vector3 _direction, float _distanceFocus)
        {
            origine = _origine;
            height = _height;
            width = _width;
            direction = Vector3.Normalize(_direction);
            distanceFocus = _distanceFocus;
            focus = new Vector3(origine.X + (width / 2), origine.Y + (height / 2), origine.Z);
            focus = Vector3.Add(focus, Vector3.Multiply(Vector3.Negate(direction), distanceFocus));
        }

        public Vector3 GetFocusAngle(float x, float y)
        {
            Vector3 res = Vector3.Subtract(new Vector3(x, y, origine.Z), focus);
            return Vector3.Normalize(res);
        }
    }
}