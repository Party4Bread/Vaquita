using Android.App;
using Android.Widget;
using Android.OS;
using Orca;
using Orca.vm;

namespace RE_Vaquita
{
    [Activity(Label = "RE_Vaquita", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView (Resource.Layout.Main);

            Button execute = (Button)FindViewById(Resource.Id.button1);
            EditText code = (EditText)FindViewById(Resource.Id.editText1);
            TextView text = (TextView)FindViewById(Resource.Id.textView1);
            execute.Click += delegate
            {
                var parser = new Parser();
                var program = parser.compile(code.Text);
                var machine = new Machine();
                machine.load(program);
                machine.run();
                text.Text = machine.io.console;
            };
        }
    }
}

