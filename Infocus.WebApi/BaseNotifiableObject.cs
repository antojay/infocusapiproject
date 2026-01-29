using System;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading;

namespace Infocus.Common
{
    [Serializable]
    public abstract class BaseNotifiableObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        protected virtual void FirePropertyChangedEvent(String propertyName)
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;   
            if(dispatcher != null && !dispatcher.CheckAccess())
            {
                Dispatcher.CurrentDispatcher.BeginInvoke
                (
                    (ThreadStart)(() => 
                    {
                        if(PropertyChanged != null)
                        {
                            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                        }
                    })
                );
            }
            else
            {
                if(PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            
        }

        protected virtual void FirePropertyChangingEvent(String propertyName)
        {
            if(PropertyChanging != null)
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                if(dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.BeginInvoke
                    (
                        (ThreadStart)(() => 
                            {
                                if(PropertyChanging != null)
                                {
                                    PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
                                }
                            })
                    );
                }
                else
                {
                    if(PropertyChanging != null)
                    {
                        PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
                    }
                }
            }
        }
        public virtual event PropertyChangedEventHandler PropertyChanged;
        public virtual event PropertyChangingEventHandler PropertyChanging;
    }
}
