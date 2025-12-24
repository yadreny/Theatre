using System;
using UnityEngine;

namespace AlSo
{
    /// <summary>
    /// Плавный 2D freeform-калькулятор весов для Locomotion:
    /// - Вход: произвольный набор точек (SpeedX, SpeedZ) через IFreedomWeightedSource.Points.
    /// - Выход: массив весов той же длины (сумма ≈ 1), максимально гладкий по углу и радиусу.
    ///
    /// Идея:
    /// - по углу: вес ~ max(dot(dir, clipDir), 0), где dir — направление скорости,
    ///   clipDir — нормализованный вектор точки клипа;
    /// - по радиусу: клипы, у которых |Points[i]| ближе к |point|, получают больший вес;
    /// - idle (точки около (0,0)) автоматически выигрывают при маленькой скорости.
    /// </summary>
    public class FreedomWeightCalculator
    {
        private readonly IFreedomWeightedSource _source;

        // Копия точек из источника (чтобы не лазить за ними каждый кадр).
        private Vector2[] _points = Array.Empty<Vector2>();

        // Максимальный радиус среди всех точек (для нормализации радиальной части).
        private float _maxRadius = 1f;

        public FreedomWeightCalculator(IFreedomWeightedSource source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            Rebuild();
        }

        /// <summary>
        /// Обновить локальный кэш точек и пересчитать максимальный радиус.
        /// Вызывается из конструктора и должен вызываться вручную, если Points меняются.
        /// </summary>
        public void Rebuild()
        {
            _points = _source.Points ?? Array.Empty<Vector2>();

            _maxRadius = 0f;
            for (int i = 0; i < _points.Length; i++)
            {
                float r = _points[i].magnitude;
                if (r > _maxRadius)
                {
                    _maxRadius = r;
                }
            }

            if (_maxRadius < 1e-4f)
            {
                _maxRadius = 1f;
            }
        }

        /// <summary>
        /// Гизмо: рисуем точки как сферы (для дебага расположения клипов в плоскости).
        /// Вызывать из OnDrawGizmos() владельца.
        /// </summary>
        public void DragGizmos()
        {
            if (_points == null || _points.Length == 0)
            {
                return;
            }

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.yellow;

            for (int i = 0; i < _points.Length; i++)
            {
                Vector2 p = _points[i];
                Vector3 pos = new Vector3(p.x, 0f, p.y);
                Gizmos.DrawSphere(pos, 0.05f);
            }
        }

        /// <summary>
        /// Главная функция: по заданной точке (SpeedX, SpeedZ) возвращает массив весов.
        /// Длина массива = числу точек источника, сумма весов ≈ 1.
        /// </summary>
        public float[] GetWeights(Vector2 point)
        {
            int count = _points != null ? _points.Length : 0;
            if (count == 0)
            {
                return Array.Empty<float>();
            }

            // Один клип — просто вес 1
            if (count == 1)
            {
                return new[] { 1f };
            }

            float[] weights = new float[count];

            float r = point.magnitude;
            const float eps = 1e-5f;

            // Если скорость почти нулевая — отдаём всё клипу, ближайшему к центру (обычно idle).
            if (r < eps)
            {
                int bestIndex = 0;
                float bestRadius = float.MaxValue;

                for (int i = 0; i < count; i++)
                {
                    float clipR = _points[i].magnitude;
                    if (clipR < bestRadius)
                    {
                        bestRadius = clipR;
                        bestIndex = i;
                    }
                }

                weights[bestIndex] = 1f;
                return weights;
            }

            // Основной режим: учитываем угол + радиус.
            Vector2 dir = point / r;

            float sum = 0f;

            for (int i = 0; i < count; i++)
            {
                Vector2 clipPoint = _points[i];
                float clipR = clipPoint.magnitude;

                // Если точка клипа почти в нуле — считаем её "радиальным idle",
                // она учитывает только радиальное совпадение, без угла.
                if (clipR < eps)
                {
                    float radialIdle = 1f - Mathf.Clamp01(r / _maxRadius);
                    if (radialIdle <= 0f)
                    {
                        weights[i] = 0f;
                        continue;
                    }

                    float wIdle = radialIdle;
                    weights[i] = wIdle;
                    sum += wIdle;
                    continue;
                }

                // Угловая часть: cos(theta) между направлением скорости и направлением клипа.
                Vector2 clipDir = clipPoint / clipR;
                float angleDot = Vector2.Dot(dir, clipDir);
                if (angleDot <= 0f)
                {
                    // Клипы, смотрящие "назад" относительно направления, не участвуют.
                    weights[i] = 0f;
                    continue;
                }

                // Радиальная часть: чем ближе по длине к текущему r, тем больше вклад.
                // 0 — далеко по радиусу, 1 — идеально совпадает.
                float radialFactor = 1f - Mathf.Clamp01(Mathf.Abs(r - clipR) / _maxRadius);
                if (radialFactor <= 0f)
                {
                    weights[i] = 0f;
                    continue;
                }

                // Итоговый "4D-подобный" вес: угол * радиус.
                float w = angleDot * radialFactor;

                weights[i] = w;
                sum += w;
            }

            // Если всё обнулилось (например, все клипы позади) — fallback: ближайший по расстоянию.
            if (sum < eps)
            {
                int bestIndex = 0;
                float bestDist = float.MaxValue;

                for (int i = 0; i < count; i++)
                {
                    Vector2 p = _points[i];
                    float dx = p.x - point.x;
                    float dy = p.y - point.y;
                    float d2 = dx * dx + dy * dy;

                    if (d2 < bestDist)
                    {
                        bestDist = d2;
                        bestIndex = i;
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    weights[i] = (i == bestIndex) ? 1f : 0f;
                }

                return weights;
            }

            // Нормализация весов.
            float invSum = 1f / sum;
            for (int i = 0; i < count; i++)
            {
                weights[i] *= invSum;
            }

            return weights;
        }
    }
}
