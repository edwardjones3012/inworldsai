using UnityEngine;

public static class VectorExtensions {

    public static Vector3 VectorMa (Vector3 _start, float _scale, Vector3 _direction) {

        var _dest = new Vector3 (
            _start.x + _direction.x * _scale,
            _start.y + _direction.y * _scale,
            _start.z + _direction.z * _scale
        );

        return _dest;

    }

}