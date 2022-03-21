using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace FinalWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string _allowedFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\..\Resources\wordle-nyt-allowed-guesses.txt");
        static string _answerFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\..\Resources\wordle-nyt-answers-alphabetical.txt");
        Brush _brushWrong = (Brush)new BrushConverter().ConvertFrom("#AA336A"); //Wrong
        Brush _brushClose = (Brush)new BrushConverter().ConvertFrom("#E3963E"); //Close
        Brush _brushRight = (Brush)new BrushConverter().ConvertFrom("#132F09"); //Right
        Brush _brushReset = (Brush)new BrushConverter().ConvertFrom("#607E54"); //Reset
        Brush _brushLight = (Brush)new BrushConverter().ConvertFrom("#B6D7A8"); //Light
        string _alphabet = "QWERTYUIOPASDFGHJKLZXCVBNM";
        private DispatcherTimer _timer;
        private TimeSpan _time;
        private TimeSpan _pb;

        private Random rng = new Random();
        private int[] _currentLetter = new int[2] { 0, 0 };
        private string _currentWord = "";
        private string _answerWord;
        private int _winProgress = 0;
        private int _bestStreak = 0;
        private int _currentStreak = 0;
        private int _currentBounce = 1;
        private bool _gameOver = false;
        private bool _won = false;
        private bool _reset = true;

        public MainWindow()
        {
            InitializeComponent();
            _answerWord = File.ReadLines(_answerFilePath).Skip(rng.Next(2308)).Take(1).First().ToUpper();
        }

        private void restart_onClick(object sender, MouseButtonEventArgs e)
        {
            txtTimer.Content = "To Start: Enter a 5 letter word";
            _currentLetter = new int[2] { 0, 0 };
            _currentWord = "";
            _answerWord = File.ReadLines(_answerFilePath).Skip(rng.Next(2308)).Take(1).First().ToUpper();
            _winProgress = 0;
            _gameOver = false;
            _reset = true;
            resetAllLetters();
            resetColors();
        }

        private void close_onClick(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void resetAllLetters()
        {
            int[] _location;
            Label _txt;
            Border _border;

            for (int _row = 0; _row < 6; _row++)
            {
                for (int _column = 0; _column < 5; _column++)
                {
                    void completedAnimation(object s, EventArgs e)
                    {
                        //if (_reset)
                        //{
                        //    _currentBounce = 1;
                        //}
                        checkAnswer(getColumn(new int[2] { _row, _column }), _column);
                        SingleShake(new int[2] { _row, _column }).Begin();
                        //strBrds[_currentBounce].Completed -= completedAnimation;
                    }

                    _location = new int[2] { _row, _column };
                    _txt = (Label)getColumn(_location).FindName("txt");
                    SingleShake(_location).Completed -= completedAnimation;
                    _border = (Border)getColumn(_location).FindName("brdBase");
                    _border.BorderBrush = _brushLight;
                    _border.ClearValue(BackgroundProperty);
                    _txt.Content = '\0';
                }
            }
        }

        /// <summary>
        /// Moves _currentLetter forward to the next available slot.
        /// </summary>
        /// <returns>True or False whether or not the move was successful.</returns>
        private bool nextLetter()
        {
            bool _successful = false;
            int _currentColumn = _currentLetter[1];

            if (_currentColumn < 5)
            {
                _currentLetter = new int[2] { _currentLetter[0], _currentColumn + 1 };
                _successful = true;
            }

            return _successful;
        }
        
        /// <summary>
        /// Moves _currentLetter back to the previous available slot.
        /// </summary>
        /// <returns>True or False whether or not the move was successful.</returns>
        private bool previousLetter()
        {
            bool _successful = false;
            int _currentColumn = _currentLetter[1];

            if (_currentColumn > 0)
            {
                _currentLetter = new int[2] { _currentLetter[0], _currentColumn - 1 };
                _successful = true;
            }

            return _successful;
        }

        private bool nextRow()
        {
            bool _successful = false;
            int _currentRow = _currentLetter[0];

            if (_currentRow < 5)
            {
                _currentLetter = new int[2] { _currentRow + 1, 0 };
                _successful = true;
            } else //Last row, don't advance but check for final answer.
            {
                _successful = true;
                _gameOver = true;
            }

            return _successful;
        }

        private UserControls.WordleColumn getColumn(int[] _focusLetter)
        {
            UserControls.WordleRow _row;
            UserControls.WordleColumn _column;

            _row = (UserControls.WordleRow)FindName("row" + _focusLetter[0]);
            _column = (UserControls.WordleColumn)_row.FindName("column" + _focusLetter[1]);

            return _column;
        }

        /// <summary>
        /// Set the Windows.Controls.Label.Text of the "_focusLetter" specified using the char specified by "_keyPressed".
        /// If there's no char specified, removes the char from the specified Windows.Controls.Label
        /// </summary>
        /// <param name="_focusLetter">Desired Windows.Controls.Label represented by an int[2]: { row, column }.</param>
        /// <param name="_keyPressed">Desired char to be set, clears char if not specified.</param>
        private void setLetter(int[] _focusLetter, char _keyPressed) //Add letter
        {
            Label _txt;

            try
            {
                _txt = (Label)getColumn(_focusLetter).FindName("txt");

                _currentWord += _keyPressed;
                _txt.Content = _keyPressed;
            } catch (Exception)
            {
                txtTimer.Content = "Somethin' Went Wrong..."; //-----------------------------------------------------------
            }
        }
        /// <summary>
        /// Set the Windows.Controls.Label.Text of the "_focusLetter" specified using the char specified by "_keyPressed".
        /// If there's no char specified, removes the char from the specified Windows.Controls.Label
        /// </summary>
        /// <param name="_focusLetter">Desired Windows.Controls.Label represented by an int[2]: { row, column }.</param>
        private void setLetter(int[] _focusLetter) //Subtract Letter
        {
            Label _txt;

            try
            {
                _txt = (Label)getColumn(_focusLetter).FindName("txt");

                _currentWord = _currentWord.Substring(0, _currentWord.Length - 1);
                _txt.Content = "";
            }
            catch (Exception)
            {
                txtTimer.Content = "Somethin' Went Wrong..."; //-----------------------------------
            }
        }

        /// <summary>
        /// Find if the _currentWord is in either the allowed list or answer list using MainWindow.findInTxtList().
        /// </summary>
        /// <returns>True or False whether or not the search is successful.</returns>
        private bool checkWord()
        {
            bool _allowedWord = false;
            bool _correctWord = false;

            if (_currentWord.Length == 5)
            {
                _allowedWord = findInTxtList(_allowedFilePath);
                _correctWord = findInTxtList(_answerFilePath);
            }

            return (_allowedWord || _correctWord);
        }

        /// <summary>
        /// Compare's _currentWord with all words in desired file.
        /// </summary>
        /// <param name="_path">File path of desired file.</param>
        /// <returns>True or False whether or not the word was found.</returns>
        private bool findInTxtList(string _path)
        {
            bool _successful = false;

            string _currentLine;
            string _lowerWord = _currentWord.ToLower();

            string _file = _path;
            StreamReader sr = new StreamReader(_file);

            while ((_currentLine = sr.ReadLine()) != null)
            {
                if (_currentLine.Contains(_lowerWord))
                {
                    _successful = true;
                }
            }

            sr.Close();

            return _successful;
        }

        private bool checkAnswer(int _row)
        {
            bool _successful = false;
            UserControls.WordleColumn _wordleColumn;
            Label _txt;

            for (int _column = 0; _column < 5; _column++) //Loop through all columns
            {
                _wordleColumn = getColumn(new int[2] { _row, _column });
                _txt = (Label)_wordleColumn.FindName("txt");
                if (_answerWord.Contains((char)_txt.Content)) //If letter is in answer
                {
                    if (_answerWord[_column] == (char)_txt.Content) //If letter is in correct spot
                    {
                        _winProgress++;
                    }
                }
            }

            return _successful;
        }

        private void checkAnswer(UserControls.WordleColumn _column, int _columnID)
        {
            int _situation = 0; //0: Letter not found, 1: Letter found, 2: Letter match!
            Label _txt = (Label)_column.FindName("txt");
            char _letter = (char)_txt.Content;

            if (_answerWord.Contains(_letter)) //If letter is in answer
            {
                _situation++;
                if (_answerWord[_columnID] == _letter) //If letter is in correct spot
                {
                    _situation++;
                }
            }

            Debug.WriteLine(_columnID + ", " + _letter);
            setColor(_column, _situation, _letter);
        }

        private void setColor(UserControls.WordleColumn _wordleColumn, int _situation, char _letter)
        {
            Border _border = (Border)_wordleColumn.FindName("brdBase");
            Border _keyboardLetter = (Border)_keyboard.FindName("brd" + _letter);

            switch (_situation)
            {
                case 0:
                    _border.BorderBrush = _brushWrong;
                    _border.Background = _brushWrong;
                    _keyboardLetter.Background = _brushWrong;
                    break;
                case 1:
                    _border.BorderBrush = _brushClose;
                    _border.Background = _brushClose;
                    _keyboardLetter.Background = _brushClose;
                    break;
                case 2:
                    _border.BorderBrush = _brushRight;
                    _border.Background = _brushRight;
                    _keyboardLetter.Background = _brushRight;
                    break;
                case 3:
                    _border.BorderBrush = _brushReset;
                    _border.Background = _brushReset;
                    _keyboardLetter.Background = _brushReset;
                    break;

            }
            //#607E54 unused - mid green
            //#132F09 wrong - dark green
            //# FFD687 close - orange
            //# FC8B99 right - pink
        }

        private void resetColors()
        {
            Border _keyboardLetter;
            foreach (char _letter in _alphabet)
            {
                _keyboardLetter = (Border)_keyboard.FindName("brd" + _letter);
                _keyboardLetter.Background = _brushReset;
            }
        }

        private void OnKeyPressDown(object sender, KeyEventArgs e)
        {
            if (_gameOver)
            {
                return;
            }

            int[] _focusLetter = _currentLetter;
            //Use to convert to ACCURATE key; All other methods I tried failed to convert or failed in returning accurate keys
            KeyConverter kc = new KeyConverter();

            //Try parse to letter if not, skip, if so then check if letter
            if (char.TryParse(kc.ConvertToString(e.Key), out char _keyPressed) && char.IsLetter(_keyPressed))
            {
                if(nextLetter())
                {
                    setLetter(_focusLetter, _keyPressed);
                    SingleShake(_focusLetter).Begin();
                }
            } else if (e.Key == Key.Back)
            {
                if (previousLetter())
                {
                    setLetter(_currentLetter);
                    SingleShake(_currentLetter).Begin();
                }
            } else if (e.Key == Key.Enter)
            {
                //Does word exist in either list
                if (checkWord())
                {
                    //Advance to next row
                    if (nextRow())
                    {
                        if (_reset)
                        {
                            startTimer();
                            _reset = false;
                        }
                        BounceRow(_focusLetter[0]);
                        checkAnswer(_focusLetter[0]);

                        if (_winProgress == _answerWord.Length)
                        {
                            _timer.Stop();
                            txtTimer.Content = "You Did It! Time: " + _time.ToString("c").Substring(4);
                            _currentStreak++;
                            if (_currentStreak > _bestStreak)
                            {
                                _bestStreak = _currentStreak;
                                txtStreak.Content = "Streak: " + _bestStreak;
                            }
                            if (_time > _pb)
                            {
                                _pb = _time;
                                txtPR.Content = "Personal Best: " + _time.ToString("c").Substring(4);
                            }
                            _gameOver = true;
                        }
                        else if (_gameOver)
                        {
                            _currentStreak = 0;
                            _timer.Stop();
                            txtTimer.Content = "Answer: " + _answerWord + "! Time: " + _time.ToString("c").Substring(4);
                        }

                        _currentWord = "";
                        _winProgress = 0;
                    }
                } else
                {
                    WrongShake(_currentLetter[0]).Begin();
                }
            }
        }

        private Storyboard WrongShake(int _rowID)
        {
            UserControls.WordleRow _row;
            Grid _grid;

            _row = (UserControls.WordleRow)FindName("row" + _rowID);
            _grid = (Grid)_row.FindName("grdBase");

            Storyboard strBrdWrongShake = (Storyboard)_grid.Resources["WrongShake"];
            Storyboard.SetTarget(strBrdWrongShake.Children.ElementAt(0) as DoubleAnimationUsingKeyFrames, _grid);
            return strBrdWrongShake;
        }

        private Storyboard SingleShake(int[] _focusLetter)
        {
            Border _border = (Border)getColumn(_focusLetter).FindName("brdBase");
            Storyboard strBrdSingleShake = (Storyboard)_border.Resources["SingleShake"];
            Storyboard.SetTarget(strBrdSingleShake.Children.ElementAt(0) as DoubleAnimationUsingKeyFrames, _border);
            return strBrdSingleShake;
        }

        private void BounceRow(int _rowID)
        {
            UserControls.WordleRow _row;
            List<Storyboard> strBrds = new List<Storyboard>();

            //void completedAnimation(object s, EventArgs e)
            //{
            //    if (_reset)
            //    {
            //        _currentBounce = 1;
            //    }
            //    Debug.WriteLine(strBrds[_currentBounce]);
            //    checkAnswer(getColumn(new int[2] { _rowID, _currentBounce }), _currentBounce);
            //    strBrds[_currentBounce++].Begin();
            //    //strBrds[_currentBounce].Completed -= completedAnimation;
            //}

            _row = (UserControls.WordleRow)FindName("row" + _rowID);

            Storyboard strBrdBegin = SingleShake(new int[2] { _rowID, 0});
            strBrds.Add(strBrdBegin);

            for(int _columnID = 1; _columnID < 5; _columnID++)
            {
                Storyboard strBrdSequence = SingleShake(new int[2] { _rowID, _columnID });
                strBrds.Add(strBrdSequence);
            }

            for (int _strBrdID = 0; _strBrdID < 4; _strBrdID++)
            {
                strBrds[_strBrdID].Completed += completedAnimation;
                //    (s, e) =>
                //{
                //    if (_reset)
                //    {
                //        _currentBounce = 1;
                //    }
                //    checkAnswer(getColumn(new int[2] { _rowID, _currentBounce }), _currentBounce);
                //    strBrds[_currentBounce++].Begin();
                //};
            }

            //strBrds[_currentBounce].Completed += (s, e) =>
            //{
            //    //checkAnswer(getColumn(new int[2] { _rowID, _currentBounce }), _currentBounce);
            //};

            checkAnswer(getColumn(new int[2] { _rowID, 0 }), 0);
            _currentBounce = 1;
            strBrds[0].Begin();
        }

        private void startTimer()
        {
            _time = TimeSpan.FromSeconds(60);

            _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                txtTimer.Content = "Time: " + _time.ToString("c").Substring(4);
                if (_time == TimeSpan.Zero)
                {
                    _timer.Stop();
                    txtTimer.Content = "Answer: " + _answerWord;
                    _currentStreak = 0;
                    _gameOver = true;
                }
                _time = _time.Add(TimeSpan.FromSeconds(-1));
            }, Application.Current.Dispatcher);

            _timer.Start();
        }

        private void completedAnimation(object s, EventArgs e)
        {
            ClockGroup cg = s as ClockGroup;
            Storyboard sb = (Storyboard)cg.Timeline;
            Timeline tl = sb.Children.First();
            Border b = (Border)Storyboard.GetTarget(tl);
            UserControls.WordleColumn wc = (UserControls.WordleColumn)b.Parent;
            Grid g = (Grid)wc.Parent;
            UserControls.WordleRow wr = (UserControls.WordleRow)g.Parent;
            int.TryParse(wr.Name.Substring(wr.Name.Length - 1), out int _rowID);
            int.TryParse(wc.Name.Substring(wc.Name.Length - 1), out int _columnID);

            _columnID++;
            checkAnswer(getColumn(new int[2] { _rowID, _columnID }), _columnID);
            SingleShake(new int[2] { _rowID, _columnID }).Begin();
            SingleShake(new int[2] { _rowID, _columnID - 1 }).Completed -= completedAnimation;
            //Holy shit, it worked
        }

        private void border_mouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
