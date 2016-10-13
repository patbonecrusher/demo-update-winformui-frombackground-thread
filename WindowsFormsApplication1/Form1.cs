using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// This is for the Thread way pre C#5 and .Net 4.5
    /// </summary>
    public static class ControlExtension
    {
        private delegate void SetPropertyThreadSafeDelegate<TResult>(
             Control @this,
             Expression<Func<TResult>> property,
             TResult value);

        public static void SetPropertyThreadSafe<TResult>(
            this Control @this,
            Expression<Func<TResult>> property,
            TResult value)
        {
            var propertyInfo = (property.Body as MemberExpression).Member
                as PropertyInfo;

            if (propertyInfo == null ||
                !@this.GetType().IsSubclassOf(propertyInfo.ReflectedType) ||
                @this.GetType().GetProperty(
                    propertyInfo.Name,
                    propertyInfo.PropertyType) == null)
            {
                throw new ArgumentException("The lambda expression 'property' must reference a valid property on this Control.");
            }

            if (@this.InvokeRequired)
            {
                @this.Invoke(new SetPropertyThreadSafeDelegate<TResult>
                (SetPropertyThreadSafe),
                new object[] { @this, property, value });
            }
            else
            {
                @this.GetType().InvokeMember(
                    propertyInfo.Name,
                    BindingFlags.SetProperty,
                    null,
                    @this,
                    new object[] { value });
            }
        }
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            // This is the Task way C# >= 5 and .Net >= 4.5
            var progress = new Progress<string>(s => label1.Text = "using task: " + s);
            await Task.Factory.StartNew(() =>
            {
                // Perform a long running work...
                for (var i = 0; i < 10; i++)
                {
                    ((IProgress<string>)progress).Report(i.ToString());
                    Task.Delay(500).Wait();
                }

            }, TaskCreationOptions.LongRunning);
            label1.Text = "completed";

            // This is the Thread way pre C# < 5 and .Net < 4.5
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                for (var i = 0; i < 10; i++)
                {
                    label1.SetPropertyThreadSafe(() => label1.Text, "using thread: " + i.ToString());
                    Thread.Sleep(500);
                }

                /* run your code here */
                label1.SetPropertyThreadSafe(() => label1.Text, "completed");
            }).Start();
        }
    }
}
