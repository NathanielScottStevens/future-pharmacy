using UnityEngine;
using UnityEngine.Assertions;

[System.Serializable]
public class HeightMap : ScriptableObject
{
    public float[] map;
    public int height;
    public int width;

    public float this[int x, int y]
    {
        get { return map[ToIndex(x, y)]; }
        set { map[ToIndex(x, y)] = value; }
    }

    public float this[float x, float y]
    {
        get
        {
            Assert.IsTrue(x >= 0 && x <= 1);
            Assert.IsTrue(y >= 0 && y <= 1);
            return map[ToIndex(Mathf.RoundToInt(x * width),
                               Mathf.RoundToInt(y * height))];
        }
        set
        {
            Assert.IsTrue(x >= 0 && x <= 1);
            Assert.IsTrue(y >= 0 && y <= 1);
            map[ToIndex(Mathf.RoundToInt(x * width),
                        Mathf.RoundToInt(y * height))] = value;
        }
    }

    public float this[int i]
    {
        get { return map[i]; }
        set { value = Mathf.Clamp01(value); map[i] = value; }
    }

    // This will only work with power of 2 dimensions!
    int ToIndex(int x, int y)
    {
        return (x & (width - 1)) + (y & (height - 1)) * width;
    }

    public void Init(int width, int height)
    {
        Assert.IsTrue(Mathf.IsPowerOfTwo(width), "Width must be a power of 2.");
        Assert.IsTrue(Mathf.IsPowerOfTwo(height), "Height must be a power of 2.");
        this.width = width;
        this.height = height;
        map = new float[width * height];
    }

    public Texture2D GetTexture()
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] colors = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sample = this[x, y];
                colors[y * height + x] = new Color(sample, sample, sample);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    public void SetFromTexture(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();
        map = new float[pixels.Length];
        width = texture.width;
        height = texture.height;

        for (int i = 0; i < pixels.Length; i++)
        {
            map[i] = pixels[i].grayscale;
        }
    }

    public void Sine(float freq, float phase)
    {
        // Shift sine to [0, 1] range: sin(x)/2 + 0.5
        freq = 0.1f;
        Mathf.Clamp(phase, 0, 2 * Mathf.PI);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float v = (Mathf.Sin(2 * Mathf.PI * x) / 2 * 0.5f) * (Mathf.Sin(2 * Mathf.PI * y) / 2 * 0.5f);
                this[x, y] = v;
            }
        }
    }

    public void AddCircle(float radius, float weight, int h, int k)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Use equation of circle (x - h)^2 + (y - k)^2 = r^2
                if ((x - h)*(x - h) + (y - k)*(y - k) < radius * radius)
                {
                    this[x, y] += weight;
                }
            }
        }
    }

    public void AddCone(float radius, float weight, int h, int k)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Use equation of circle (x - h)^2 + (y - k)^2 = r^2

                float distFromCenter = Mathf.Sqrt((x - h) * (x - h) + (y - k) * (y - k));
                if (distFromCenter < radius)
                {
                    this[x, y] += weight * ((radius - distFromCenter) / radius);
                }
            }
        }
    }

    /// <summary>
    /// Adds Perlin noise.
    /// </summary>
    /// <param name="scale">The amount of the perlin texture that is used.</param>
    /// <param name="weight">The max height of the noise generated.</param>
    /// <param name="octaves">The number of iterations at lesser weights.</param>
    public void AddPerlin(float scale, float weight, int octaves=1)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float xCoord = (float)x / width * scale;
                float yCoord = (float)y / height * scale;
                float sample = 0;

                float a = 1;
                for (int i = 0; i <= octaves; i++)
                {
                    float mult = Mathf.Pow(2, i);
                    sample += (a) * Mathf.PerlinNoise(mult * xCoord, mult * yCoord) * weight;
                    a /= 2;
                }

                this[x, y] += sample;
            }
        }
    }

    public void MaskCircle(float radius)
    {
        int h = width / 2;
        int k = height / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distFromCenter = Mathf.Sqrt((x - h) * (x - h) + (y - k) * (y - k));
                if (distFromCenter >= 0)
                    this[x, y] *= ((radius - distFromCenter) / radius);
                else
                    this[x, y] = 0;
            }
        }
    }

    public void FadeEdges(int distance)
    {
        // Bottom
        for (int y = 0; y < distance; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float curve = (Mathf.Sin(x) / 2f) + 0.5f;
                this[x, y] *= ((float)y / distance) * curve;
            }
        }

        // Top
        for (int y = height - distance; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                this[x, y] *= ((float)height - y) / distance; 
            }
        }

        // Left
        for (int x = 0; x < distance; x++)
        {
            for (int y = 0; y < height; y++)
            {
                this[x, y] *= (float)x / distance;
            }
        }

        // Right
        for (int x = width - distance; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                this[x, y] *= ((float)width - x) / distance;
            }
        }
    }

    float JaggedCliffCurve(int x)
    {
        return ((Mathf.Sin(x * Random.Range(0.25f, 1f)) / 2f) + 0.5f);
    }

    float ErodedCliffCurve(int x)
    {
        return (Mathf.Sin(x) / 2f) + 0.5f;
    }

    public void SmoothAverage()
    {
        for (int y = 0; y < height; y += 2)
        {
            for (int x = 0; x < width; x += 2)
            {
                float ave = (this[x, y] +
                             this[x, y + 1] +
                             this[x + 1, y] +
                             this[x + 1, y + 1])
                             / 4f;
                this[x, y] = ave;
            }
        }
    }

    public void SmoothHigh()
    {
        for (int y = 0; y < height; y += 2)
        {
            for (int x = 0; x < width; x += 2)
            {
                float[] values = { this[x, y],
                                   this[x, y + 1],
                                   this[x + 1, y],
                                   this[x + 1, y + 1] };

                this[x, y] = Mathf.Max(values);
            }
        }
    }

    public void SmoothLow()
    {
        for (int y = 0; y < height; y += 2)
        {
            for (int x = 0; x < width; x += 2)
            {
                float[] values = { this[x, y],
                                   this[x, y + 1],
                                   this[x + 1, y],
                                   this[x + 1, y + 1] };

                this[x, y] = Mathf.Min(values);
            }
        }
    }
}