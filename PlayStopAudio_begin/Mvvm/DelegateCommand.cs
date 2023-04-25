using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PlayStopAudio_begin.Mvvm
{
    class DelegateCommand : ICommand
    {
        private readonly Action executeAction;

        public DelegateCommand(Action executeAction)
        {
            this.executeAction = executeAction;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            executeAction();
        }

        public event EventHandler CanExecuteChanged;
    }
}
