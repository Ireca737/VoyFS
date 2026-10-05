#if !CONSOLE
using System.Drawing;
using System.Windows.Forms;

namespace JoinFS.FlyLab.UI
{
    internal static class FlyLabFlightPlanChrome
    {
        static readonly Color Paper = Color.FromArgb(238, 239, 224);
        static readonly Color Field = Color.FromArgb(255, 255, 248);
        static readonly Color Ink = Color.FromArgb(28, 43, 55);
        static readonly Color Caption = Color.FromArgb(55, 72, 82);
        static readonly Color Rule = Color.FromArgb(113, 127, 130);

        public static void Apply(Form form)
        {
            if (form == null) return;
            form.SuspendLayout();
            form.BackColor = Paper; form.ForeColor = Ink;
            form.Font = new Font("Segoe UI", 9F);
            form.ClientSize = new Size(620, 570);

            Label lCall=Find<Label>(form,"label1"), lType=Find<Label>(form,"label2"),
                lFrom=Find<Label>(form,"label3"), lTo=Find<Label>(form,"label4"),
                lRoute=Find<Label>(form,"label5"), lRules=Find<Label>(form,"label6"),
                lRemarks=Find<Label>(form,"label7"), lAlt=Find<Label>(form,"Label_Altitude"),
                lSB=Find<Label>(form,"label8"), lStatus=Find<Label>(form,"Label_SimBriefStatus");
            TextBox call=Find<TextBox>(form,"Text_Callsign"), type=Find<TextBox>(form,"Text_Type"),
                from=Find<TextBox>(form,"Text_From"), to=Find<TextBox>(form,"Text_To"),
                route=Find<TextBox>(form,"Text_Route"), remarks=Find<TextBox>(form,"Text_Remarks"),
                alt=Find<TextBox>(form,"Text_Altitude"), sb=Find<TextBox>(form,"Text_SimBriefUsername");
            ComboBox rules=Find<ComboBox>(form,"Combo_Rules");
            Button import=Find<Button>(form,"Button_ImportSimBrief"), clear=Find<Button>(form,"Button_Clear"),
                ok=Find<Button>(form,"Button_OK"), cancel=Find<Button>(form,"Button_Cancel");

            form.Controls.Add(new Label {
                Name="FlyLab_FplHeading", Text="FLIGHT PLAN  /  ICAO FPL", AutoSize=false,
                Location=new Point(24,16), Size=new Size(390,30),
                Font=new Font("Segoe UI Semibold",16F,FontStyle.Bold), ForeColor=Ink, BackColor=Color.Transparent
            });
            form.Controls.Add(new Label {
                Name="FlyLab_FplBrand", Text="FlyLabFS", AutoSize=false, TextAlign=ContentAlignment.MiddleRight,
                Location=new Point(450,18), Size=new Size(145,26),
                Font=new Font("Segoe UI Semibold",10F,FontStyle.Bold),
                ForeColor=Color.FromArgb(24,92,126), BackColor=Color.Transparent
            });
            AddRule(form,24,51,571);

            PlaceLabel(lCall,24,67,165,"CALLSIGN"); PlaceLabel(lRules,216,67,120,"FLIGHT RULES");
            PlaceLabel(lType,363,67,232,"AIRCRAFT (ICAO COD)");
            PlaceField(call,24,88,165); PlaceCombo(rules,216,88,120); PlaceField(type,363,88,232);

            PlaceLabel(lFrom,24,129,165,"DEPARTURE"); PlaceLabel(lTo,216,129,165,"DESTINATION");
            PlaceLabel(lAlt,408,129,187,"CRUISING ALTITUDE");
            PlaceField(from,24,150,165); PlaceField(to,216,150,165); PlaceField(alt,408,150,187);

            PlaceLabel(lRoute,24,191,571,"ROUTE"); PlaceField(route,24,212,571);
            PlaceLabel(lRemarks,24,253,571,"REMARKS"); PlaceField(remarks,24,274,571);

            PlaceLabel(lSB,24,318,80,"SIMBRIEF"); PlaceField(sb,112,315,270);
            PlaceButton(import,395,314,200,30,true);
            if(import!=null) import.Text="IMPORTA DA SIMBRIEF";
            if(lStatus!=null) {
                lStatus.Location=new Point(112,448); lStatus.Size=new Size(483,20);
                lStatus.ForeColor=Color.FromArgb(24,92,126); lStatus.BackColor=Color.Transparent;
                lStatus.Font=new Font("Segoe UI Semibold",8.5F,FontStyle.Bold);
            }

            AddRule(form,24,371,571);
            PlaceButton(clear,24,386,145,32,false); PlaceButton(cancel,365,386,105,32,false);
            PlaceButton(ok,482,386,113,32,true);
            if(clear!=null) clear.Text="AZZERA PIANO";
            if(cancel!=null) cancel.Text="ANNULLA";
            if(ok!=null) ok.Text="INVIA FPL";

            form.ResumeLayout(false); form.PerformLayout();
        }

        static T Find<T>(Form form,string name) where T:Control {
            Control[] m=form.Controls.Find(name,true); return m.Length>0 ? m[0] as T : null;
        }
        static void AddRule(Form form,int x,int y,int width) {
            form.Controls.Add(new Panel { Location=new Point(x,y), Size=new Size(width,1), BackColor=Rule });
        }
        static void PlaceLabel(Label l,int x,int y,int w,string text) {
            if(l==null)return; l.Text=text; l.AutoSize=false; l.Location=new Point(x,y); l.Size=new Size(w,18);
            l.BackColor=Color.Transparent; l.ForeColor=Caption;
            l.Font=new Font("Segoe UI Semibold",8F,FontStyle.Bold);
        }
        static void PlaceField(TextBox f,int x,int y,int w) {
            if(f==null)return; f.Location=new Point(x,y); f.Size=new Size(w,26); f.BackColor=Field; f.ForeColor=Ink;
            f.BorderStyle=BorderStyle.FixedSingle; f.Font=new Font("Consolas",10F);
        }
        static void PlaceMultiField(TextBox f,int x,int y,int w,int h) {
            if(f==null)return; f.Location=new Point(x,y); f.Size=new Size(w,h); f.BackColor=Field; f.ForeColor=Ink;
            f.BorderStyle=BorderStyle.FixedSingle; f.Font=new Font("Consolas",10F);
            f.Multiline=true; f.ScrollBars=ScrollBars.Vertical; f.AcceptsReturn=true;
        }
        static void PlaceCombo(ComboBox c,int x,int y,int w) {
            if(c==null)return; c.Location=new Point(x,y); c.Size=new Size(w,26); c.BackColor=Field; c.ForeColor=Ink;
            c.FlatStyle=FlatStyle.Flat; c.Font=new Font("Consolas",10F);
        }
        static void PlaceButton(Button b,int x,int y,int w,int h,bool primary) {
            if(b==null)return; b.Location=new Point(x,y); b.Size=new Size(w,h);
            b.BackColor=primary?Color.FromArgb(24,92,126):Color.FromArgb(221,224,211);
            b.ForeColor=primary?Color.White:Ink; b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderSize=1;
            b.FlatAppearance.BorderColor=primary?Color.FromArgb(24,92,126):Rule;
            b.UseVisualStyleBackColor=false; b.Font=new Font("Segoe UI Semibold",8.5F,FontStyle.Bold);
        }
    }
}
#endif
