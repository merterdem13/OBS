using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    public partial class StudentNotesOverlay : UserControl
    {
        private GlobalState _globalState => GlobalState.Instance;

        public StudentNotesOverlay()
        {
            InitializeComponent();
            _globalState.PropertyChanged += GlobalState_PropertyChanged;
        }

        private void GlobalState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlobalState.IsStudentNotesOverlayVisible))
            {
                if (_globalState.IsStudentNotesOverlayVisible)
                {
                    this.Visibility = Visibility.Visible;
                    var storyboard = (Storyboard)FindResource("FadeInStoryboard");
                    // Eğer text inputa odaklanmak isterseniz:
                    // Dispatcher.InvokeAsync(() => NoteInputBox.Focus(), System.Windows.Threading.DispatcherPriority.Background);
                    storyboard.Begin();
                }
                else
                {
                    CloseAnimated();
                }
            }
        }

        private void CloseAnimated()
        {
            var storyboard = (Storyboard)FindResource("FadeOutStoryboard");
            storyboard.Completed += (s, ev) =>
            {
                this.Visibility = Visibility.Collapsed;
            };
            storyboard.Begin();
        }

        private void Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            _globalState.CloseStudentNotesCommand.Execute(null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _globalState.CloseStudentNotesCommand.Execute(null);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                BtnSaveNote_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void BtnSaveNote_Click(object sender, RoutedEventArgs e)
        {
            var vm = _globalState.SelectedStudentForNotes;
            if (vm == null || string.IsNullOrWhiteSpace(vm.NewNoteText)) return;

            var repo = new DataAccess.StudentNoteRepository();
            var newNote = repo.AddNote(vm.StudentNumber, vm.NewNoteText);
            
            vm.Notes.Insert(0, newNote);
            vm.SpecialNote = vm.NewNoteText; // HasNote tetiklemesi
            vm.NewNoteText = string.Empty;
        }

        private void BtnDeleteNote_Click(object sender, RoutedEventArgs e)
        {
            var vm = _globalState.SelectedStudentForNotes;
            if (vm == null) return;

            if (sender is Button btn && btn.DataContext is Models.StudentNote note)
            {
                // Animasyon için ana kapsayıcıyı (Border) bul
                var border = FindAncestor<Border>(btn);
                if (border != null)
                {
                    // RenderTransform'un TranslateTransform olduğundan emin ol
                    if (!(border.RenderTransform is TranslateTransform))
                    {
                        border.RenderTransform = new TranslateTransform(0, 0);
                    }

                    var storyboard = new Storyboard();
                    
                    // Saydamlık azalması
                    var opacityAnim = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.3)));
                    Storyboard.SetTarget(opacityAnim, border);
                    Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(UIElement.OpacityProperty));

                    // Sağa doğru kayma
                    var slideAnim = new DoubleAnimation(0, 40, new Duration(TimeSpan.FromSeconds(0.3)));
                    slideAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
                    Storyboard.SetTarget(slideAnim, border);
                    Storyboard.SetTargetProperty(slideAnim, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

                    storyboard.Children.Add(opacityAnim);
                    storyboard.Children.Add(slideAnim);
                    
                    // Animasyon bittiğinde koleksiyondan sil
                    storyboard.Completed += (s, ev) => 
                    {
                        var repo = new DataAccess.StudentNoteRepository();
                        repo.DeleteNote(note.Id, vm.StudentNumber);
                        
                        vm.Notes.Remove(note);
                        
                        if (vm.Notes.Count > 0)
                            vm.SpecialNote = vm.Notes[0].NoteText;
                        else
                            vm.SpecialNote = string.Empty;
                    };
                    
                    storyboard.Begin();
                }
                else
                {
                    // Fallback: Border bulunamazsa direkt sil
                    var repo = new DataAccess.StudentNoteRepository();
                    repo.DeleteNote(note.Id, vm.StudentNumber);
                    vm.Notes.Remove(note);
                }
            }
        }

        private static T? FindAncestor<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);
            if (parent == null) return null;
            if (parent is T parentT) return parentT;
            return FindAncestor<T>(parent);
        }
    }
}
