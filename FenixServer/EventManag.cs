using System;
using WeifenLuo.WinFormsUI.Docking;

namespace FenixServer
{
    public partial class EventManag : DockContent
    {
        public EventManag()
        {
            InitializeComponent();
        }

        //Dodanie Eventu
        public void addEvent(AlarmEvent ev)
        {
            //Synchronizacja
            dgvMain.Invoke(new Action<AlarmEvent>((ev1) =>
            {
                dgvMain.Rows.Add();
                dgvMain.Rows[dgvMain.Rows.Count - 1].Cells["alDate"].Value = ev1.Tm;
                dgvMain.Rows[dgvMain.Rows.Count - 1].Cells["alInfo"].Value = ev1.Mess;
            }), ev);
        }
    }
}