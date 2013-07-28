using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace StandAloneComplex
{
    /// <summary>
    /// 音声認識器
    /// </summary>
    class SpeechRecognizer : IDisposable
    {
        /// <summary>
        /// Dispose済みフラグ
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// 音声認識エンジン
        /// </summary>
        private SpeechRecognitionEngine speechEngin;

        /// <summary>
        /// 音声認識テキストと顔のタイプのテーブル
        /// </summary>
        private Dictionary<string, FaceTypes> faceTypeTable = new Dictionary<string, FaceTypes>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SpeechRecognizer()
        {
            faceTypeTable.Add("解除", FaceTypes.Unknown);
            faceTypeTable.Add("笑い男", FaceTypes.LaughingMan);
            faceTypeTable.Add("ミッキーマウス", FaceTypes.MickeyMouse);
            faceTypeTable.Add("キィテイーちゃん", FaceTypes.HelloKitty);
            faceTypeTable.Add("トモダチ", FaceTypes.Tomodachi);
            faceTypeTable.Add("石仮面", FaceTypes.StoneMask);
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~SpeechRecognizer()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// リソースを開放する
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソースを開放する（マネージリソースの開放をオプションにより切り替え）
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed) { return; }

            if (disposing)
            {
                // マネージリソースの開放
            }

            this.Stop();

            this.disposed = true;
        }

        /// <summary>
        /// 音声認識エンジンの初期化を行う
        /// </summary>
        /// <param name="kinect"></param>
        public bool Start(KinectSensor kinect)
        {
            if (this.speechEngin != null) { return false; }

            var info = this.GetRecognizer("ja-JP");
            if (info == null) { return false; }

            var choices = new Choices();
            foreach (var item in this.faceTypeTable)
            {
                choices.Add(item.Key);
            }

            var builder = new GrammarBuilder();
            builder.Culture = info.Culture;
            builder.Append(choices);
            var grammar = new Grammar(builder);

            this.speechEngin = new SpeechRecognitionEngine(info.Id);
            this.speechEngin.LoadGrammar(grammar);
            this.speechEngin.SpeechRecognized += this.speechEngin_SpeechRecognized;

            kinect.AudioSource.NoiseSuppression = true;
            kinect.AudioSource.EchoCancellationMode = EchoCancellationMode.CancellationAndSuppression;

            var stream = kinect.AudioSource.Start();
            this.speechEngin.SetInputToAudioStream(stream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            this.speechEngin.RecognizeAsync(RecognizeMode.Multiple);

            return true;
        }

        /// <summary>
        /// 音声認識を停止する
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (this.speechEngin == null) { return false; }

            this.speechEngin.SpeechRecognized -= this.speechEngin_SpeechRecognized;
            this.speechEngin.RecognizeAsyncCancel();
            this.speechEngin.Dispose();
            this.speechEngin = null;

            return true;
        }

        /// <summary>
        /// Cultureの名前で指定した音声認識エンジンを取得する
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private RecognizerInfo GetRecognizer(string name)
        {
            return SpeechRecognitionEngine.InstalledRecognizers().Where(
                r => name.Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        /// <summary>
        /// 単語を認識したときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void speechEngin_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (0.8 < e.Result.Confidence)
            {
                this.OnSpeechRecognized(new SpeechEventArgs(this.faceTypeTable[e.Result.Text]));
            }
        }

        /// <summary>
        /// 音声認識のイベント
        /// </summary>
        public event EventHandler<SpeechEventArgs> SpeechRecognized;

        /// <summary>
        /// 音声認識のイベント発行
        /// </summary>
        /// <param name="e"></param>
        private void OnSpeechRecognized(SpeechEventArgs e)
        {
            if (this.SpeechRecognized != null)
            {
                this.SpeechRecognized(this, e);
            }
        }
    }

    /// <summary>
    /// 顔のタイプ
    /// </summary>
    enum FaceTypes
    {
        Unknown,
        LaughingMan,
        MickeyMouse,
        HelloKitty,
        Tomodachi,
        StoneMask
    }

    /// <summary>
    /// 音声認識のイベント引数
    /// </summary>
    class SpeechEventArgs : EventArgs
    {
        /// <summary>
        /// 顔のタイプ
        /// </summary>
        public FaceTypes FaceType { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="faceType"></param>
        public SpeechEventArgs(FaceTypes faceType)
        {
            this.FaceType = faceType;
        }
    }
}
