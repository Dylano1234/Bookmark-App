using Bookmark_App.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Bookmark_App.ViewModels
{
    public partial class ListViewModel : BaseViewModel
    {
        public Models.List _list;
        public string Title => _list.title;
        public ItemStatus Status { get; set; }

        public ICommand SetStatusCommand { get; }
        public ListViewModel(Models.List list)
        {
            _list = list;
            SetStatusCommand = new RelayCommand<ItemStatus>(SetStatus);
        }
        private void SetStatus(ItemStatus status)
        {
            Status = status;
            OnPropertyChanged(nameof(Status));
        }
    }
}
