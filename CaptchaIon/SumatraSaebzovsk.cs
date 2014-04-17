using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CaptchaIon
{
    public class CaptchaImage1 : IBytesImage
    {
        static class Rng
        {
            private static readonly byte[] Randb = new byte[4];
            private static readonly RNGCryptoServiceProvider Rand = new RNGCryptoServiceProvider();
            public static int Next()
            {
                Rand.GetBytes(Randb);
                var value = BitConverter.ToInt32(Randb, 0);
                if (value < 0) value = -value;
                return value;
            }
            public static int Next(int max)
            {
                Rand.GetBytes(Randb);
                var value = BitConverter.ToInt32(Randb, 0);
                value = value % (max + 1);
                if (value < 0) value = -value;
                return value;
            }
            public static int Next(int min, int max)
            {
                var value = Next(max - min) + min;
                return value;
            }
        }
        public Color BackgroundColor
        {
            get { return _bc; }
        }

        public Bitmap Image { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        private Color _bc;

        public CaptchaImage1(string s, Color bc, int width, int height)
        {
            _bc = bc;
            Width = width;
            Height = height;
            GenerateImage(s);
        }

        ~CaptchaImage1()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Image.Dispose();
        }
        private readonly FontFamily[] _fonts = {
            new FontFamily("Times New Roman"),
            new FontFamily("Georgia"),
            new FontFamily("Arial"),
            new FontFamily("Comic Sans MS")
        };
        private void GenerateImage(string s)
        {
            const int threshold = 150;
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(bitmap);
            var rect = new Rectangle(0, 0, Width, Height);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(_bc))
            {
                g.FillRectangle(b, rect);
            }
            var emSize = Width * 4 / s.Length;
            var family = _fonts[Rng.Next(_fonts.Length - 1)];
            var font = new Font(family, emSize);
            const int fontStyle = (int)FontStyle.Bold;
            var measured = new SizeF(0, 0);
            var workingSize = new SizeF(Width, Height);
            while (emSize > 2 &&
                    (measured = g.MeasureString(s, font)).Width > workingSize.Width ||
                    measured.Height > workingSize.Height)
            {
                font.Dispose();
                font = new Font(family, emSize -= 2);
            }
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            for (var i = 0; i < 25; i++)
            {
                var x1 = Rng.Next(Width);
                var x2 = Rng.Next(Width);
                var y1 = Rng.Next(Height);
                var y2 = Rng.Next(Height);

                g.DrawLine(new Pen(Color.Silver), x1, y1, x2, y2);
            }
            var path = new GraphicsPath();
            path.AddString(s, font.FontFamily, fontStyle, font.Size, rect, format);
            var bcR = Convert.ToInt32(_bc.R);
            int red = Rng.Next(255), green = Rng.Next(255), blue = Rng.Next(255);
            while (red >= bcR && red - threshold <= bcR ||
                red < bcR && red + threshold >= bcR)
            {
                red = Rng.Next(0, 255);
            }
            var sBrush = new SolidBrush(Color.FromArgb(red, green, blue));
            g.FillPath(sBrush, path);
            double distort = Rng.Next(5, 10) * (Rng.Next(10) == 1 ? 1 : -1);
            using (var copy = (Bitmap)bitmap.Clone())
            {
                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        var newX = (int)(x + (distort * Math.Sin(Math.PI * y / 84.0)));
                        var newY = (int)(y + (distort * Math.Cos(Math.PI * x / 44.0)));
                        if (newX < 0 || newX >= Width) newX = 0;
                        if (newY < 0 || newY >= Height) newY = 0;
                        bitmap.SetPixel(x, y, copy.GetPixel(newX, newY));
                    }
                }
            }
            font.Dispose();
            sBrush.Dispose();
            g.Dispose();
            Image = bitmap;
        }

        public byte[] GetBytesImage()
        {
            using (var bitmap=Image)
            {
                return UtilsCaptcha.ImageToByte(bitmap);
            }
           
        }
    }

    public class CaptchaImage2 : IBytesImage
    {
        public string Text { get; private set; }
        public Bitmap Image { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        private string _familyName;
        private readonly Random _random = new Random();
        public CaptchaImage2(string s, int width, int height)
        {
            Text = s;
            SetDimensions(width, height);
            GenerateImage();
        }
        public CaptchaImage2(string s, int width, int height, string familyName)
        {
            Text = s;
            SetDimensions(width, height);
            SetFamilyName(familyName);
            GenerateImage();
        }
        ~CaptchaImage2()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Image.Dispose();
        }
        private void SetDimensions(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width", width, "Argument out of range, must be greater than zero.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height", height, "Argument out of range, must be greater than zero.");
            Width = width;
            Height = height;
        }
        private void SetFamilyName(string familyName)
        {
            try
            {
                var font = new Font(_familyName, 14F);
                _familyName = familyName;
                font.Dispose();
            }
            catch
            {
                _familyName = FontFamily.GenericSerif.Name;
            }
        }

        private void GenerateImage()
        {
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width, Height);
            var hatchBrush = new HatchBrush(HatchStyle.SmallConfetti, Color.LightGray, Color.White);
            g.FillRectangle(hatchBrush, rect);
            SizeF size;
            float fontSize = rect.Height + 1;
            Font font;
            do
            {
                fontSize--;
                font = new Font(_familyName, fontSize, FontStyle.Bold);
                size = g.MeasureString(Text, font);
            } while (size.Width > rect.Width);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            var path = new GraphicsPath();
            path.AddString(Text, font.FontFamily, (int)font.Style, font.Size, rect, format);
            const float v = 8F;
            PointF[] points =
            {
                new PointF(_random.Next(rect.Width) / v, _random.Next(rect.Height) / v),
                new PointF(rect.Width - _random.Next(rect.Width) / v, _random.Next(rect.Height) / v),
                new PointF(_random.Next(rect.Width) / v, rect.Height - _random.Next(rect.Height) / v),
                new PointF(rect.Width - _random.Next(rect.Width) / v, rect.Height - _random.Next(rect.Height) / v)
            };
            var matrix = new Matrix();
            matrix.Translate(0F, 0F);
            path.Warp(points, rect, matrix, WarpMode.Perspective, 0F);
            hatchBrush = new HatchBrush(HatchStyle.LargeConfetti, Color.Black, Color.Black);
            g.FillPath(hatchBrush, path);
            var m = Math.Max(rect.Width, rect.Height);
            for (var i = 0; i < (int)(rect.Width * rect.Height / 30F); i++)
            {
                var x = _random.Next(rect.Width);
                var y = _random.Next(rect.Height);
                var w = _random.Next(m / 50);
                var h = _random.Next(m / 50);
                g.FillEllipse(hatchBrush, x, y, w, h);
            }
            font.Dispose();
            hatchBrush.Dispose();
            g.Dispose();
            Image = bitmap;
        }

        public byte[] GetBytesImage()
        {
            using (var bitmap=Image)
            {
                return UtilsCaptcha.ImageToByte(bitmap);
            }
            
        }
    }

    public class CaptchaImage3 : IDisposable, IBytesImage
    {

        static class random
        {
            static Random _r = new Random();

            public static void Refresh(int? seed = null)
            {
                if (seed == null)
                    _r = new Random();
                if (seed != null) _r = new Random(seed.Value);
            }

            public static int GetInt(int max)
            {
                return _r.Next(max);
            }

            public static int GetInt(int min, int max)
            {
                if (min < max)
                    return _r.Next(min, max);
                return _r.Next(max, min);
            }

            public static Double GetDouble(int max = 1)
            {
                return _r.NextDouble() * max;
            }

            public static char GetChar(string example = "abcdefghijklmnopqrstuvwxyz")
            {
                if (string.IsNullOrEmpty(example)) example = " ";
                return example[GetInt(example.Length)];
            }

            public static string GetString(int size = 4, string example = "abcdefghijklmnopqrstuvwxyz")
            {
                var charArr = new char[size];
                for (var i = 0; i < charArr.Length; i++)
                    charArr[i] = GetChar(example);
                return new string(charArr);
            }

            public static T GetOne<T>(IEnumerable<T> obj)
            {
                var enumerable = obj as T[] ?? obj.ToArray();
                return enumerable.ElementAt(enumerable.Count());
            }

            public static IEnumerable<T> Sort<T>(IEnumerable<T> obj)
            {
                return obj == null ? null : obj.OrderBy(d => _r.NextDouble());
            }
        }

        readonly Bitmap _image;
        readonly Graphics _g;
        private readonly string textcode;
        public CaptchaImage3(string text,int w, int h)
        {
            textcode = text;
            _image = new Bitmap(w, h);
            _g = Graphics.FromImage(_image);
        }

        public Bitmap Model1()
        {
            string code = textcode;
            if (string.IsNullOrWhiteSpace(code))
                return null;
            _g.InterpolationMode = InterpolationMode.Low;
            _g.CompositingQuality = CompositingQuality.HighSpeed;
            _g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            _g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, _image.Width, _image.Height);
            Brush brushBack = new LinearGradientBrush(rect, Color.FromArgb(random.GetInt(150, 256), 255, 255), Color.FromArgb(255, random.GetInt(150, 256), 255), random.GetInt(90));
            _g.FillRectangle(brushBack, rect);
            for (var i = 0; i < 2; i++)
            {
                var p1 = new Point(0, random.GetInt(_image.Height));
                var p2 = new Point(random.GetInt(_image.Width), random.GetInt(_image.Height));
                var p3 = new Point(random.GetInt(_image.Width), random.GetInt(_image.Height));
                var p4 = new Point(_image.Width, random.GetInt(_image.Height));
                Point[] p = { p1, p2, p3, p4 };
                var pen = new Pen(Color.Gray, 1);
                _g.DrawBeziers(pen, p);
            }
            for (var i = 0; i < code.Length; i++)
            {
                string strChar = code.Substring(i, 1);
                int deg = random.GetInt(-15, 15);
                float x = (_image.Width / code.Length / 2) + (_image.Width / code.Length) * i;
                float y = _image.Height / 2;
                var font = new Font("Consolas", random.GetInt(16, 24), FontStyle.Regular);
                var size = _g.MeasureString(strChar, font);
                var m = new Matrix();
                m.RotateAt(deg, new PointF(x, y), MatrixOrder.Append);
                m.Shear(random.GetInt(-10, 10) * 0.03f, 0);
                _g.Transform = m;
                Brush brushPen = new LinearGradientBrush(rect, Color.FromArgb(random.GetInt(0, 256), 0, 0), Color.FromArgb(0, 0, random.GetInt(0, 256)), random.GetInt(90));
                _g.DrawString(code.Substring(i, 1), font, brushPen, new PointF(x - size.Width / 2, y - size.Height / 2));

                _g.Transform = new Matrix();
            }

            _g.Save();

            return _image;
        }

        //public System.Web.Mvc.FileContentResult model1Result(string code)
        //{
        //    var image = model1(code);
        //    MemoryStream ms = new System.IO.MemoryStream();
        //    image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
        //    return new System.Web.Mvc.FileContentResult(ms.ToArray(), "image/Gif");
        //}

        public void Dispose()
        {
            _g.Dispose();
            _image.Dispose();
        }

        public byte[] GetBytesImage()
        {
            return UtilsCaptcha.ImageToByte(Model1());
        }
    }

    public class CaptchaImage4 : IDisposable, IBytesImage
    {
        public static string DefaultFontFamily = "Arial";

        public static float DefaultFontSize = 24;

        #region Private fields
        private int _width = 100;
        private int _height = 100;
        private Bitmap _captchaImage;
        private CaptchaCharCollection _text = new CaptchaCharCollection();
        private string _simpleText;
        private HatchStyle _bgHatchStyle = HatchStyle.SmallConfetti;
        private Color _bgForeColor = Color.LightGray;
        private Color _bgBackColor = Color.White;
        private readonly Random _random = new Random();
        private Color _fontForeColor = Color.LightGray;
        private Color _fontBackColor = Color.DarkGray;
        private HatchStyle _fontHatchStyle = HatchStyle.LargeConfetti;
        #endregion

        #region Public Properties
        public HatchStyle FontHatchStyle
        {
            get { return _fontHatchStyle; }
            set { _fontHatchStyle = value; }
        }

        public Color FontBackColor
        {
            get { return _fontBackColor; }
            set { _fontBackColor = value; }
        }

        public Color FontForeColor
        {
            get { return _fontForeColor; }
            set { _fontForeColor = value; }
        }

        public Color BgBackColor
        {
            get { return _bgBackColor; }
            set { _bgBackColor = value; }
        }

        public Color BgForeColor
        {
            get { return _bgForeColor; }
            set { _bgForeColor = value; }
        }

        public HatchStyle BgHatchStyle
        {
            get { return _bgHatchStyle; }
            set { _bgHatchStyle = value; }
        }

        public string SimpleText
        {
            get { return _simpleText; }
            set
            {
                _simpleText = value;
                ProcessSimpleText(DefaultFontFamily, DefaultFontSize);
            }
        }

        public CaptchaCharCollection Text
        {
            get { return _text; }
            set
            {
                _text.Dispose();
                _text = value;
            }
        }

        public Bitmap CaptchaImage
        {
            get
            {
                if (_captchaImage == null)
                    GenerateImage();
                return _captchaImage;
            }
        }


        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }


        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }
        #endregion

        #region Ctor
        public CaptchaImage4() { }

        public CaptchaImage4(int width, int height, CaptchaCharCollection text)
        {
            _width = width;
            _height = height;
            _text = text;
            GenerateImage();
        }

        public CaptchaImage4(int width, int height, string text, string fontFamily, float pxFontSize)
        {
            _width = width;
            _height = height;
            _simpleText = text;

            ProcessSimpleText(fontFamily, pxFontSize);
            GenerateImage();
        }
        #endregion

        protected void GenerateImage()
        {
            var bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(bitmap);
            g.PageUnit = GraphicsUnit.Pixel;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, _width, _height);
            var hatchBrush = new HatchBrush(_bgHatchStyle, _bgForeColor, _bgBackColor);
            g.FillRectangle(hatchBrush, rect);


            while (true)
            {
                foreach (var cc in _text.Chars)
                    cc.Size = g.MeasureString(cc.Character, cc.Font);

                if (_text.Width < rect.Width &&
                    _text.MaxHeight < rect.Height)
                    break;

                _text.IncreaseFontSize(-1);

                if (_text.Width <= 0 || _text.MaxHeight <= 0)
                    return;
            }



            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };


            var path = new GraphicsPath();
            var original = new Point(_width - _text.Width, _text.MaxHeight / 2);
            foreach (var cc in _text.Chars.Where(cc => cc != null))
            {
                path.AddString(cc.Character, cc.Font.FontFamily, (int)cc.Font.Style, cc.Font.Size, original, format);
                original.X += (int)cc.Size.Width;
            }

            const float v = 4F;
            PointF[] points =
                      {
                        new PointF(
                          _random.Next(rect.Width) / v,
                          _random.Next(rect.Height) / v),
                        new PointF(
                          rect.Width - _random.Next(rect.Width) / v,
                          _random.Next(rect.Height) / v),
                        new PointF(
                          _random.Next(rect.Width) / v,
                          rect.Height - _random.Next(rect.Height) / v),
                        new PointF(
                          rect.Width - _random.Next(rect.Width) / v,
                          rect.Height - _random.Next(rect.Height) / v)
                      };

            var matrix = new Matrix();
            matrix.Translate(0F, 0F);
            path.Warp(points, rect, matrix, WarpMode.Perspective, 0F);



            hatchBrush = new HatchBrush(_fontHatchStyle, _fontForeColor, _fontBackColor);
            g.FillPath(hatchBrush, path);



            var m = Math.Max(rect.Width, rect.Height);
            for (var i = 0; i < (int)(rect.Width * rect.Height / 30F); i++)
            {
                var x = _random.Next(rect.Width);
                var y = _random.Next(rect.Height);
                var w = _random.Next(m / 50);
                var h = _random.Next(m / 50);
                g.FillEllipse(hatchBrush, x, y, w, h);
            }



            hatchBrush.Dispose();
            g.Dispose();
            _text.Dispose();

            _captchaImage = bitmap;
        }


        private void ProcessSimpleText(string fontFamily, float pxFontSize)
        {
            var chars = _simpleText.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                _text.Chars.Add(new CaptchaChar(fontFamily, pxFontSize, chars[i].ToString()));
            }
        }



        ~CaptchaImage4()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _captchaImage != null)
                _captchaImage.Dispose();
        }

    
public byte[] GetBytesImage()
{
   return  UtilsCaptcha.ImageToByte(_captchaImage);
}
}

    public class CaptchaCharCollection : IDisposable
    {
        private readonly List<CaptchaChar> _chars = new List<CaptchaChar>();

        public int Width
        {
            get
            {
                return _chars.Sum(cc => (int)cc.Size.Width);
            }
        }
        public int MaxHeight
        {
            get
            {
                return _chars.Select(cc => (int)cc.Size.Height).Concat(new[] { 0 }).Max();
            }

        }

        public List<CaptchaChar> Chars
        {
            get { return _chars; }
        }

        public void IncreaseFontSize(float pxIncrement)
        {
            foreach (var cc in _chars)
            {
                cc.IncreaseFontSize(pxIncrement);
            }
        }

        public void Add(CaptchaChar captchaChar)
        {
            _chars.Add(captchaChar);
        }

        #region IDisposable Members

        public void Dispose()
        {
            foreach (var cc in _chars)
                cc.Dispose();
        }

        #endregion
    }
    public class CaptchaChar : IDisposable
    {
        private Font _font = new Font("Arial", 12, GraphicsUnit.Pixel);
        private string _character = string.Empty;
        private SizeF _size = new Size();

        public SizeF Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public Font Font
        {
            get { return _font; }
        }

        public string Character
        {
            get { return _character; }
            set { _character = value; }
        }

        public CaptchaChar()
        {
            _font = new Font("Arial", 12, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        public CaptchaChar(string fontFamily, float pxFontSize, string character)
        {
            _font = new Font(fontFamily, pxFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            _character = character;
        }

        public void IncreaseFontSize(float pxSizeDelta)
        {
            _font.Dispose();
            _font = new Font(_font.FontFamily, _font.Size + pxSizeDelta, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_font != null)
                _font.Dispose();
        }

        #endregion
    }

    public class CaptchaImage5:IBytesImage
    {
        public enum FontWarpFactor
        {
            None,
            Low,
            Medium,
            High,
            Extreme
        }

        public enum BackgroundNoiseLevel
        {
            None,
            Low,
            Medium,
            High,
            Extreme
        }

        public enum LineNoiseLevel
        {
            None,
            Low,
            Medium,
            High,
            Extreme
        }


        private static readonly string[] RandomFontFamily = { "arial", "arial black", "comic sans ms", "courier new", "estrangelo edessa", "franklin gothic medium", "georgia", "lucida console", "lucida sans unicode", "mangal", "microsoft sans serif", "palatino linotype", "sylfaen", "tahoma", "times new roman", "trebuchet ms", "verdana" };

        private static readonly Color[] RandomColor = { Color.Red, Color.Green, Color.Blue, Color.Black, Color.Purple, Color.Orange };

        public static string TextChars { get; set; }

        public static int TextLength { get; set; }

        public static FontWarpFactor FontWarp { get; set; }

        public static BackgroundNoiseLevel BackgroundNoise { get; set; }

        public static LineNoiseLevel LineNoise { get; set; }

        public static double CacheTimeOut { get; set; }



        private int _height;
        private int _width;
        private readonly Random _rand;


        public string UniqueId { get; private set; }

        public DateTime RenderedAt { get; private set; }

        public string Text { get; set; }

        public int Width
        {
            get { return _width; }
            set
            {
                if ((value <= 60))
                    throw new ArgumentOutOfRangeException("width", value, "width must be greater than 60.");

                _width = value;
            }
        }

        public int Height
        {
            get { return _height; }
            set
            {
                if (value <= 30)
                    throw new ArgumentOutOfRangeException("height", value, "height must be greater than 30.");

                _height = value;
            }
        }



        static CaptchaImage5()
        {
            FontWarp = FontWarpFactor.Medium;
            BackgroundNoise = BackgroundNoiseLevel.Low;
            LineNoise = LineNoiseLevel.Low;
            TextLength = 5;
            TextChars = "ACDEFGHJKLNPQRTUVXYZ2346789";
            CacheTimeOut = 180D;
        }


        public CaptchaImage5()
        {
            _rand = new Random();
            Width = 180;
            Height = 50;
            Text = GenerateRandomText();
            RenderedAt = DateTime.Now;
            UniqueId = Guid.NewGuid().ToString("N");
        }
        public Bitmap RenderImage()
        {
            return GenerateImagePrivate();
        }
        private string GetRandomFontFamily()
        {
            return RandomFontFamily[_rand.Next(0, RandomFontFamily.Length)];
        }

        private string GenerateRandomText()
        {
            var sb = new StringBuilder(TextLength);
            var maxLength = TextChars.Length;
            for (var n = 0; n <= TextLength - 1; n++)
                sb.Append(TextChars.Substring(_rand.Next(maxLength), 1));
            return sb.ToString();
        }

        private PointF RandomPoint(int xmin, int xmax, int ymin, int ymax)
        {
            return new PointF(_rand.Next(xmin, xmax), _rand.Next(ymin, ymax));
        }

        private Color GetRandomColor()
        {
            return RandomColor[_rand.Next(0, RandomColor.Length)];
        }

        private PointF RandomPoint(Rectangle rect)
        {
            return RandomPoint(rect.Left, rect.Width, rect.Top, rect.Bottom);
        }

        private static GraphicsPath TextPath(string s, Font f, Rectangle r)
        {
            var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
            var gp = new GraphicsPath();
            gp.AddString(s, f.FontFamily, (int)f.Style, f.Size, r, sf);
            return gp;
        }

        private Font GetFont()
        {
            float fsize;
            var fname = GetRandomFontFamily();

            switch (FontWarp)
            {
                case FontWarpFactor.None:
                    goto default;
                case FontWarpFactor.Low:
                    fsize = Convert.ToInt32(_height * 0.8);
                    break;
                case FontWarpFactor.Medium:
                    fsize = Convert.ToInt32(_height * 0.85);
                    break;
                case FontWarpFactor.High:
                    fsize = Convert.ToInt32(_height * 0.9);
                    break;
                case FontWarpFactor.Extreme:
                    fsize = Convert.ToInt32(_height * 0.95);
                    break;
                default:
                    fsize = Convert.ToInt32(_height * 0.7);
                    break;
            }
            return new Font(fname, fsize, FontStyle.Bold);
        }

        private Bitmap GenerateImagePrivate()
        {
            var bmp = new Bitmap(_width, _height, PixelFormat.Format24bppRgb);

            using (var gr = Graphics.FromImage(bmp))
            {
                gr.SmoothingMode = SmoothingMode.AntiAlias;
                gr.Clear(Color.White);

                var charOffset = 0;
                double charWidth = _width / TextLength;

                foreach (var c in Text)
                {
                    using (var fnt = GetFont())
                    {
                        using (Brush fontBrush = new SolidBrush(GetRandomColor()))
                        {
                            var rectChar = new Rectangle(Convert.ToInt32(charOffset * charWidth), 0, Convert.ToInt32(charWidth), _height);
                            var gp = TextPath(c.ToString(CultureInfo.InvariantCulture), fnt, rectChar);
                            WarpText(gp, rectChar);
                            gr.FillPath(fontBrush, gp);
                            charOffset += 1;
                        }
                    }
                }

                var rect = new Rectangle(new Point(0, 0), bmp.Size);
                AddNoise(gr, rect);
                AddLine(gr, rect);
            }

            return bmp;
        }

        private void WarpText(GraphicsPath textPath, Rectangle rect)
        {
            float warpDivisor;
            float rangeModifier;

            switch (FontWarp)
            {
                case FontWarpFactor.None:
                    goto default;
                case FontWarpFactor.Low:
                    warpDivisor = 6F;
                    rangeModifier = 1F;
                    break;
                case FontWarpFactor.Medium:
                    warpDivisor = 5F;
                    rangeModifier = 1.3F;
                    break;
                case FontWarpFactor.High:
                    warpDivisor = 4.5F;
                    rangeModifier = 1.4F;
                    break;
                case FontWarpFactor.Extreme:
                    warpDivisor = 4F;
                    rangeModifier = 1.5F;
                    break;
                default:
                    return;
            }

            var rectF = new RectangleF(Convert.ToSingle(rect.Left), 0, Convert.ToSingle(rect.Width), rect.Height);

            var hrange = Convert.ToInt32(rect.Height / warpDivisor);
            var wrange = Convert.ToInt32(rect.Width / warpDivisor);
            var left = rect.Left - Convert.ToInt32(wrange * rangeModifier);
            var top = rect.Top - Convert.ToInt32(hrange * rangeModifier);
            var width = rect.Left + rect.Width + Convert.ToInt32(wrange * rangeModifier);
            var height = rect.Top + rect.Height + Convert.ToInt32(hrange * rangeModifier);

            if (left < 0)
                left = 0;
            if (top < 0)
                top = 0;
            if (width > Width)
                width = Width;
            if (height > Height)
                height = Height;

            var leftTop = RandomPoint(left, left + wrange, top, top + hrange);
            var rightTop = RandomPoint(width - wrange, width, top, top + hrange);
            var leftBottom = RandomPoint(left, left + wrange, height - hrange, height);
            var rightBottom = RandomPoint(width - wrange, width, height - hrange, height);

            var points = new[] { leftTop, rightTop, leftBottom, rightBottom };
            var m = new Matrix();
            m.Translate(0, 0);
            textPath.Warp(points, rectF, m, WarpMode.Perspective, 0);
        }
        private void AddNoise(Graphics g, Rectangle rect)
        {
            int density;
            int size;

            switch (BackgroundNoise)
            {
                case BackgroundNoiseLevel.None:
                    goto default;
                case BackgroundNoiseLevel.Low:
                    density = 30;
                    size = 40;
                    break;
                case BackgroundNoiseLevel.Medium:
                    density = 18;
                    size = 40;
                    break;
                case BackgroundNoiseLevel.High:
                    density = 16;
                    size = 39;
                    break;
                case BackgroundNoiseLevel.Extreme:
                    density = 12;
                    size = 38;
                    break;
                default:
                    return;
            }

            var br = new SolidBrush(GetRandomColor());
            int max = Convert.ToInt32(Math.Max(rect.Width, rect.Height) / size);

            for (var i = 0; i <= Convert.ToInt32((rect.Width * rect.Height) / density); i++)
                g.FillEllipse(br, _rand.Next(rect.Width), _rand.Next(rect.Height), _rand.Next(max), _rand.Next(max));

            br.Dispose();
        }

        private void AddLine(Graphics g, Rectangle rect)
        {
            int length;
            float width;
            int linecount;

            switch (LineNoise)
            {
                case LineNoiseLevel.None:
                    goto default;
                case LineNoiseLevel.Low:
                    length = 4;
                    width = Convert.ToSingle(_height / 31.25);
                    linecount = 1;
                    break;
                case LineNoiseLevel.Medium:
                    length = 5;
                    width = Convert.ToSingle(_height / 27.7777);
                    linecount = 1;
                    break;
                case LineNoiseLevel.High:
                    length = 3;
                    width = Convert.ToSingle(_height / 25);
                    linecount = 2;
                    break;
                case LineNoiseLevel.Extreme:
                    length = 3;
                    width = Convert.ToSingle(_height / 22.7272);
                    linecount = 3;
                    break;
                default:
                    return;
            }

            var pf = new PointF[length + 1];
            using (var p = new Pen(GetRandomColor(), width))
            {
                for (var l = 1; l <= linecount; l++)
                {
                    for (var i = 0; i <= length; i++)
                        pf[i] = RandomPoint(rect);

                    g.DrawCurve(p, pf, 1.75F);
                }
            }
        }

        byte[] IBytesImage.GetBytesImage()
        {
            using (var bitmap=RenderImage())
            return  UtilsCaptcha.ImageToByte(bitmap);
        }
    }

    public class CaptchaImage6 : IBytesImage
    {

        private readonly Random _random;
        private int _width;
        private int _height;

        private int _randomTextLength;
        private string _randomTextChars;
        private string _fontFamilyName;

        private readonly string _guid;

        public CaptchaImage6()
        {
            _random = new Random();

            FontWarp = FontWarpFactor.Low;
            BackgroundNoise = BackgroundNoiseLevel.Low;
            LineNoise = LineNoiseLevel.None;
            Width = 180;
            Height = 50;
            FontWhitelist = "arial;arial black;comic sans ms;courier new;estrangelo edessa;franklin gothic medium;" +
                "georgia;lucida console;lucida sans unicode;mangal;microsoft sans serif;palatino linotype;" +
                "sylfaen;tahoma;times new roman;trebuchet ms;verdana";
            _randomTextLength = 5;
            _randomTextChars = "ACDEFGHJKLMNPQRTUVWXY34679";
            _fontFamilyName = string.Empty;
            Text = GenerateRandomText();
            RenderedAt = DateTime.Now;
            _guid = Guid.NewGuid().ToString();
        }

        public enum FontWarpFactor
        {
            None, Low, Medium, High, Extreme
        }
        public enum BackgroundNoiseLevel
        {
            None, Low, Medium, High, Extreme
        }
        public enum LineNoiseLevel
        {
            None, Low, Medium, High, Extreme
        }

        #region Properties

        public string UniqueId
        {
            get
            {
                return _guid;
            }
        }

        public DateTime RenderedAt { get; private set; }

        public string FontFamilyName
        {
            get
            {
                return _fontFamilyName;
            }
            set
            {
                var font = new Font(value, 12);
                font.Dispose();
                _fontFamilyName = font.Name == FontFamily.GenericSansSerif.Name ? font.Name : value;
            }
        }

        public FontWarpFactor FontWarp { get; set; }
        public BackgroundNoiseLevel BackgroundNoise { get; set; }
        public LineNoiseLevel LineNoise { get; set; }
        public string TextChars
        {
            get
            {
                return _randomTextChars;
            }

            set
            {
                _randomTextChars = value;
                Text = GenerateRandomText();
            }
        }

        public int TextLength
        {
            get
            {
                return _randomTextLength;
            }

            set
            {
                _randomTextLength = value;
                Text = GenerateRandomText();
            }
        }

        public string Text { get; private set; }
        public int Width
        {
            get
            {
                return _width;
            }

            set
            {
                if (value <= 60)
                {
                    throw new ArgumentOutOfRangeException("Width", value, "width must be greater than 60.");
                }
                _width = value;
            }
        }
        public int Height
        {
            get
            {
                return _height;
            }

            set
            {
                if (value <= 30)
                {
                    throw new ArgumentOutOfRangeException("Height", value, "height must be greater than 30.");
                }
                _height = value;
            }
        }

        public string FontWhitelist { get; set; }

        #endregion
        public Bitmap GenerateImage()
        {
            Font font = null;
            Brush brush = null;

            Bitmap bitmap;
            Graphics graphics = null;

            GraphicsPath pathWithChar = null;
            StringFormat stringFormat = null;

            try
            {
                bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
                graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;


                var rectangle = new Rectangle(0, 0, Width, Height);
                brush = new SolidBrush(Color.White);
                graphics.FillRectangle(brush, rectangle);

                var charOffset = 0;

                double charWidth = Width / TextLength;


                foreach (var c in Text)
                {
                    font = GetFont();
                    var charRectangle = new Rectangle((int)(charOffset * charWidth), 0, (int)charWidth, Height);
                    pathWithChar = new GraphicsPath();
                    stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Near
                    };
                    pathWithChar.AddString(c.ToString(CultureInfo.InvariantCulture), font.FontFamily, (int)font.Style, font.Size, charRectangle, stringFormat);
                    WarpChar(pathWithChar, charRectangle);
                    brush = new SolidBrush(Color.Black);
                    graphics.FillPath(brush, pathWithChar);
                    charOffset += 1;
                }

                AddNoise(graphics, rectangle);
                AddLines(graphics, rectangle);
            }
            finally
            {
                if (stringFormat != null)
                {
                    stringFormat.Dispose();
                }

                if (pathWithChar != null)
                {
                    pathWithChar.Dispose();
                }

                if (font != null)
                {
                    font.Dispose();
                }

                if (brush != null)
                {
                    brush.Dispose();
                }

                if (graphics != null)
                {
                    graphics.Dispose();
                }
            }

            return bitmap;
        }

        private string GenerateRandomText()
        {
            var randomTextBuilder = new StringBuilder(TextLength);

            for (var i = 0; i < TextLength; i++)
            {
                randomTextBuilder.Append(TextChars.Substring(_random.Next(TextChars.Length), 1));
            }
            return randomTextBuilder.ToString();
        }

        private PointF GenerateRandomPoint(int xmin, int xmax, int ymin, int ymax)
        {
            return new PointF(_random.Next(xmin, xmax), _random.Next(ymin, ymax));
        }
        private Font GetFont()
        {
            int fontSize;
            var fontName = FontFamilyName;

            if (string.IsNullOrEmpty(fontName))
            {
                var fontFamilies = FontWhitelist.Split(';');
                fontName = fontFamilies[_random.Next(0, fontFamilies.Length)];
            }

            switch (FontWarp)
            {
                case FontWarpFactor.None:
                    fontSize = (int)(Height * 0.7);
                    break;
                case FontWarpFactor.Low:
                    fontSize = (int)(Height * 0.8);
                    break;
                case FontWarpFactor.Medium:
                    fontSize = (int)(Height * 0.85);
                    break;
                case FontWarpFactor.High:
                    fontSize = (int)(Height * 0.9);
                    break;
                case FontWarpFactor.Extreme:
                    fontSize = (int)(Height * 0.95);
                    break;
                default:
                    throw new ArgumentException("Unknown FontWarpFactor member", "FontWarp");
            }
            return new Font(fontName, fontSize, FontStyle.Bold);
        }
        private void WarpChar(GraphicsPath pathWithText, Rectangle textRectangle)
        {
            float warpDivisor, rangeModifier;

            switch (FontWarp)
            {
                case FontWarpFactor.None:
                    return;
                case FontWarpFactor.Low:
                    warpDivisor = 6;
                    rangeModifier = 1;
                    break;
                case FontWarpFactor.Medium:
                    warpDivisor = 5;
                    rangeModifier = 1.3f;
                    break;
                case FontWarpFactor.High:
                    warpDivisor = 4.5f;
                    rangeModifier = 1.4f;
                    break;
                case FontWarpFactor.Extreme:
                    warpDivisor = 4;
                    rangeModifier = 1.5f;
                    break;
                default:
                    throw new ArgumentException("Unknown FontWarpFactor member", "FontWarp");
            }

            var textRectangleFloat = new RectangleF(textRectangle.Left, textRectangle.Top, textRectangle.Width, textRectangle.Height);

            var heightRatio = Convert.ToInt32(textRectangle.Height / warpDivisor);
            var widthRatio = Convert.ToInt32(textRectangle.Width / warpDivisor);
            var leftTextRectangle = textRectangle.Left - (int)(widthRatio * rangeModifier);
            var topTextRectangle = textRectangle.Top - (int)(heightRatio * rangeModifier);
            var widthTextRectangle = textRectangle.Left + textRectangle.Width + (int)(widthRatio * rangeModifier);
            var heightTextRectangle = textRectangle.Top + textRectangle.Height + (int)(heightRatio * rangeModifier);

            if (leftTextRectangle < 0)
            {
                leftTextRectangle = 0;
            }

            if (topTextRectangle < 0)
            {
                topTextRectangle = 0;
            }

            if (widthTextRectangle > Width)
            {
                widthTextRectangle = Width;
            }

            if (heightTextRectangle > Height)
            {
                heightTextRectangle = Height;
            }

            var leftTop = GenerateRandomPoint(leftTextRectangle, leftTextRectangle + widthRatio, topTextRectangle, topTextRectangle + heightRatio);
            var rightTop = GenerateRandomPoint(widthTextRectangle - widthRatio, widthTextRectangle, topTextRectangle, topTextRectangle + heightRatio);
            var leftBottom = GenerateRandomPoint(leftTextRectangle, leftTextRectangle + widthRatio, heightTextRectangle - heightRatio, heightTextRectangle);
            var rightBottom = GenerateRandomPoint(widthTextRectangle - widthRatio, widthTextRectangle, heightTextRectangle - heightRatio, heightTextRectangle);
            var points = new[] { leftTop, rightTop, leftBottom, rightBottom };

            var matrix = new Matrix();
            matrix.Translate(0, 0);
            pathWithText.Warp(points, textRectangleFloat, matrix, WarpMode.Perspective, 0);
            matrix.Dispose();
        }

        private void AddNoise(Graphics graphics, Rectangle rectangle)
        {
            int density, size;

            switch (BackgroundNoise)
            {
                case BackgroundNoiseLevel.None:
                    return;
                case BackgroundNoiseLevel.Low:
                    density = 30;
                    size = 40;
                    break;
                case BackgroundNoiseLevel.Medium:
                    density = 18;
                    size = 40;
                    break;
                case BackgroundNoiseLevel.High:
                    density = 16;
                    size = 39;
                    break;
                case BackgroundNoiseLevel.Extreme:
                    density = 12;
                    size = 38;
                    break;
                default:
                    throw new ArgumentException("Unknown BackgroundNoiseLevel member", "BackgroundNoise");
            }

            var brush = new SolidBrush(Color.Black);
            var max = Math.Max(rectangle.Width, rectangle.Height) / size;

            for (var i = 0; i < (rectangle.Width * rectangle.Height) / density; i++)
            {
                graphics.FillEllipse(brush, _random.Next(rectangle.Width), _random.Next(rectangle.Height), _random.Next(max), _random.Next(max));
            }

            brush.Dispose();
        }

        private void AddLines(Graphics graphics, Rectangle rectangle)
        {
            int pointsCount, lineCount;
            float lineWidth;

            switch (LineNoise)
            {
                case LineNoiseLevel.None:
                    return;
                case LineNoiseLevel.Low:
                    pointsCount = 4;
                    lineWidth = (float)(Height / 31.25);
                    lineCount = 1;
                    break;
                case LineNoiseLevel.Medium:
                    pointsCount = 5;
                    lineWidth = (float)(Height / 27.7777);
                    lineCount = 1;
                    break;
                case LineNoiseLevel.High:
                    pointsCount = 3;
                    lineWidth = Height / 25;
                    lineCount = 2;
                    break;
                case LineNoiseLevel.Extreme:
                    pointsCount = 3;
                    lineWidth = (float)(Height / 22.7272);
                    lineCount = 3;
                    break;
                default:
                    throw new ArgumentException("Unknown LineNoiseLevel member", "LineNoise");
            }

            var points = new PointF[pointsCount];
            var pen = new Pen(Color.Black, lineWidth);

            for (var i = 1; i <= lineCount; i++)
            {
                for (var j = 0; j < pointsCount; j++)
                {
                    points[j] = GenerateRandomPoint(rectangle.Left, rectangle.Width, rectangle.Top, rectangle.Bottom);
                }

                graphics.DrawCurve(pen, points, 1.75f);
            }

            pen.Dispose();
        }

        byte[] IBytesImage.GetBytesImage()
        {
            using (var bitmap=GenerateImage())
            {
                 return UtilsCaptcha.ImageToByte(bitmap);
            }
           
        }
    }
}
