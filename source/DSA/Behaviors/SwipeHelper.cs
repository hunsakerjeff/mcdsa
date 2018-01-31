using GalaSoft.MvvmLight.Messaging;
using System;
using System.Linq;
using Windows.UI.Input;
using Windows.UI.Xaml;
using DSA.Model.Dto;
using DSA.Model.Messages;

namespace DSA.Util
{
    class SwipeHelper
    {
        private const int QueueLimit = 5;
        private const int Interval = 20;
        private const double YTollerance = 60;
        private const double XSwipethreshold = 200;

        private static FixedSizedQueue<PointerPoint> Queue = new FixedSizedQueue<PointerPoint>(QueueLimit);
        private static DispatcherTimer timer = new DispatcherTimer();
        private static uint pointerId;

        public static readonly DependencyProperty ParentProperty = DependencyProperty.RegisterAttached("Parent", typeof(FrameworkElement), typeof(SwipeHelper), new PropertyMetadata(null, OnParentPropertyChanged));

        public static void SetParent(DependencyObject d, FrameworkElement value)
        {
            d.SetValue(ParentProperty, value);
        }

        public static FrameworkElement GetParent(DependencyObject d)
        {
            return (FrameworkElement)d.GetValue(ParentProperty);
        }

        private static void OnParentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
           
            var element = d as FrameworkElement;
            element.PointerMoved += (s, pe) =>
            {
                pointerId = pe.GetCurrentPoint(element).PointerId;
            };

            timer.Tick += (s, arg) => CheckPointer();
            timer.Interval = TimeSpan.FromMilliseconds(Interval);
            timer.Start();
        }

        private static void CheckPointer()
        {
            try
            {
                if (pointerId == 0)
                {
                    return;
                }

                var pointerPoint = PointerPoint.GetCurrentPoint(pointerId);

                Queue.Enqueue(pointerPoint);

                if (Queue.Count() == QueueLimit && Queue.All(pp => pp.IsInContact))
                {
                    var maxY = Queue.Max(pp => pp.Position.Y);
                    var minY = Queue.Min(pp => pp.Position.Y);
                    var firstPosition = Queue.First().Position.X;
                    var lastPosition = Queue.Last().Position.X;
                    if (Math.Abs(maxY - minY) < YTollerance && Math.Abs(firstPosition - lastPosition) > XSwipethreshold)
                    {
                        SwipeMessage message = firstPosition - lastPosition > XSwipethreshold
                                                ? SwipeMessage.Left as SwipeMessage
                                                : SwipeMessage.Right as SwipeMessage;

                        Messenger.Default.Send(message);
                        Queue.Clear();
                    }
                }
            }
            catch
            {
                // TODO add logger
            }
            
        }
    }
}
