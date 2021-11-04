using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;


/// @author  Joonas Ruuth, jkruuth
/// @version 2020
///
/// <summary>
/// Minigolf-peli, jossa tarkoituksena on saada pallo reikään mahdollisimman
/// vähäisillä lyönneillä.
/// </summary>
// TODO: taulukko ja silmukka, ks: https://tim.jyu.fi/view/kurssit/tie/ohj1/2020k/demot/demo9-tutkimus?answerNumber=3&b=Zo0K22riXhZv&size=1&task=D9B1&user=jkruuth
public class Minigolf : PhysicsGame
{
    /// <summary>
    /// Attribuuttien alustus.
    /// </summary>
    private PhysicsObject golfpallo;
    private PhysicsObject reika;
    private IntMeter lyontiLaskuri;
    private DoubleMeter voimaMittari;
    private Vector pelaajanSuunta;
    private List<Label> aloitusValikko;
    private ScoreList topLista = new ScoreList(10, true, 20);
    private Label pisteNaytto;
    private HighScoreWindow topIkkuna;
    private const double KENTAN_KOKO = 20;
    private const double MAX_NOPEUS = 1000;


    /// <summary>
    /// Pelin alustus,jossa ladataan highscoret, alustetaan pelivalikko sekä asetetaan ikkunaruutu oikean kokoiseksi.
    /// </summary>
    public override void Begin()
    {
        topLista = DataStorage.TryLoad<ScoreList>(topLista, "highscore.xml");
        PeliValikko();
        SetWindowSize(1400, 750);
    }


    /// <summary>
    /// Luodaan pelikenttä, golfpallo ja mittarit kutsuttaessa aliohjelmaa.
    /// </summary>
    private void AloitaPeli()
    {
        ClearAll();
        LuoKentta();
        LuoPistelaskuri();
        NopeusMittari();
        LuoPeliruutu();
    }


    /// <summary>
    /// Luodaan "High score"- lista.
    /// </summary>
    private void ParhaatPisteet()
    {
        topIkkuna = new HighScoreWindow(
                             "Top 10",
                             "Peli loppui! Pääsit listalle pisteillä " + lyontiLaskuri.Value + " Syötä nimesi:",
                             topLista, lyontiLaskuri.Value);
        topIkkuna.Closed += TallennaPisteet; PeliValikko();
        Add(topIkkuna);
    }


    /// <summary>
    /// HighScore-lista alkuvalikkoon
    /// </summary>
    private void ParhaatPisteetAlkuvalikko()
    {
        HighScoreWindow topIkkuna = new HighScoreWindow(
                              "Parhaat pisteet",
                              topLista);
        topIkkuna.Closed += null;
        Add(topIkkuna);
    }


    /// <summary>
    /// Aliohjelma tallentaa kutsuttaessa pelin "pisteet", eli lyönnit.
    /// </summary>
    /// <param name="sender"></param>
    private void TallennaPisteet(Window sender)
    {
        DataStorage.Save<ScoreList>(topLista, "highscore.xml");
    }


    /// <summary>
    /// Luodaan pelin alkuvalikko.
    /// </summary>
    private void PeliValikko()
    {
        ClearAll();

        aloitusValikko = new List<Label>();

        Label aloitaPeliKohta = new Label("Aloita uusi peli");
        aloitaPeliKohta.Position = new Vector(0, 40);
        aloitusValikko.Add(aloitaPeliKohta);

        Label parhaatPisteetKohta = new Label("Parhaat pisteet");
        parhaatPisteetKohta.Position = new Vector(0, 0);
        aloitusValikko.Add(parhaatPisteetKohta);

        Label poistuNappain = new Label("Lopeta peli");
        poistuNappain.Position = new Vector(0, -40);
        aloitusValikko.Add(poistuNappain);

        foreach (Label valikonKohta in aloitusValikko)
        {
            Add(valikonKohta);
        }

        Mouse.ListenOn(aloitaPeliKohta, MouseButton.Left, ButtonState.Pressed, AloitaPeli, null);
        Mouse.ListenOn(parhaatPisteetKohta, MouseButton.Left, ButtonState.Pressed, ParhaatPisteetAlkuvalikko, null) ;
        Mouse.ListenOn(poistuNappain, MouseButton.Left, ButtonState.Pressed, Exit, null);
        Mouse.ListenMovement(1.0, ValikossaLiikkuminen, null);

    }


    /// <summary>
    /// Funktio tekee valikossa liikkumisesta visuaalisemman silmukan avulla.
    /// </summary>
    private void ValikossaLiikkuminen()
    {
        foreach(Label nappain in aloitusValikko)
        {
            if (Mouse.IsCursorOn(nappain))
            {
                nappain.TextColor = Color.Green;
            }
            else
            {
                nappain.TextColor = Color.Black;
            }
        }
    }


    /// <summary>
    /// Aliohjelma luo mittarin, josta saadaan määriteltyä vauhti golfpallolle.
    /// </summary>
    private void NopeusMittari()
    {
        voimaMittari = new DoubleMeter(0);
        voimaMittari.MaxValue = MAX_NOPEUS;

        ProgressBar voimaPalkki = new ProgressBar(150, 20);
        voimaPalkki.X = -500;
        voimaPalkki.Y = -300;
        voimaPalkki.BarColor = Color.Red;
        voimaPalkki.BorderColor = Color.Red;
        voimaPalkki.BindTo(voimaMittari);

        Add(voimaPalkki);
    }

    
    /// <summary>
    /// Aliohjelma luo pistelaskurin, joka pitää kirjaa lyönneistä.
    /// </summary>
    private void LuoPistelaskuri()
    {
        lyontiLaskuri = new IntMeter(0);

        pisteNaytto = new Label();
        pisteNaytto.X = -500;
        pisteNaytto.Y = 300;
        pisteNaytto.TextColor = Color.Black;
        pisteNaytto.Color = Color.White;

        pisteNaytto.BindTo(lyontiLaskuri);
        pisteNaytto.Title = "Lyönnit";
        pisteNaytto.Font = new Font(25);

        Add(pisteNaytto);
    }


   /// <summary>
   /// Aliohjelma luo itse pelikentän, "content" kansiosta saadulla kuvalla.
   /// </summary>
    private void LuoKentta()
    {
        ColorTileMap kentta = ColorTileMap.FromLevelAsset("pelikentta1");
        kentta.SetTileMethod(Color.Black, LuoTaso, "block");
        kentta.SetTileMethod(Color.Blue, LuoTaso, "end");
        kentta.SetTileMethod(Color.White, LuoPallo);
        kentta.SetTileMethod(new Color(255, 0, 0), LuoReika);
        kentta.Optimize(Color.Black, Color.Blue);
        kentta.Execute(KENTAN_KOKO, KENTAN_KOKO);
    }


    /// <summary>
    /// Luodaan peliruutuun zoomi, poistumismahdollisuus ja taustaväri.
    /// </summary>
    private void LuoPeliruutu()
    {
        Camera.ZoomToLevel();

        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Level.Background.CreateGradient(Color.Green, Color.JungleGreen);

    }


    /// <summary>
    /// Aliohjelma luo pelikentälle seinät.
    /// </summary>
    /// <param name="paikka">Paikka, jonne taso luodaan</param>
    /// <param name="leveys">Palkin leveys</param>
    /// <param name="korkeus">Palkin korkeus</param>
    /// <param name="tekstuuri">Ladattava tekstuuri</param>
    private void LuoTaso(Vector paikka, double leveys, double korkeus, string tekstuuri)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Restitution = 1.0;
        taso.Image = LoadImage(tekstuuri);
        taso.Color = Color.JungleGreen;
        Add(taso);
    }


    /// <summary>
    /// Aliohjelma luo peliin "golfpallon", jolla peliä pelataan.
    /// </summary>
    /// <param name="paikka">Paikka, jonne pallo luodaan</param>
    /// <param name="leveys">Pallon leveys</param>
    /// <param name="korkeus">Pallon korkeus</param>
    private void LuoPallo(Vector paikka, double leveys, double korkeus)
    {
        golfpallo = new PhysicsObject(40, 40);
        golfpallo.Shape = Shape.Circle;
        golfpallo.Color = Color.White;
        golfpallo.Image = LoadImage("golfpallonuoli");
        golfpallo.Position = paikka;
        golfpallo.Restitution = 1.0;
        AddCollisionHandler(golfpallo, PalloReikaan);
        Add(golfpallo);


        golfpallo.CanRotate = false;

        Keyboard.Listen(Key.Up, ButtonState.Down, KaannaPalloa, "Kääntää palloa ylös", golfpallo, Angle.FromDegrees(1));
        Keyboard.Listen(Key.Down, ButtonState.Down, KaannaPalloa, "Kääntää palloa alas", golfpallo, Angle.FromDegrees(-1));

        Keyboard.Listen(Key.Space, ButtonState.Down, AnnaVauhti, "Määritä pallolle nopeus", golfpallo);
        Keyboard.Listen(Key.Space, ButtonState.Released, TyonnaPalloa, "Työnnä palloa", golfpallo);
    }


    /// <summary>
    /// Aliohjelma tunnistaa milloin pallo on saatu reikään.
    /// </summary>
    /// <param name="golfpallo">Pelattava pallo</param>
    /// <param name="kohde">Törmäyksen kohde</param>
    private void PalloReikaan(PhysicsObject golfpallo, PhysicsObject kohde)
    {
        if (kohde == reika)
        {
            ParhaatPisteet();
        }
    }

     
    /// <summary>
    /// Aliohjelma luo peliin reiän, jonne pallo pitää lyödä.
    /// </summary>
    /// <param name="paikka">Reiän sijainti kentällä</param>
    /// <param name="leveys">Reiän leveys</param>
    /// <param name="korkeus">Reiän korkeus</param>
    private void LuoReika(Vector paikka, double leveys, double korkeus)
    {
        reika = new PhysicsObject(60, 60);
        reika.Position = paikka;
        reika.Shape = Shape.Circle;
        reika.Color = Color.Yellow;
        reika.MakeStatic();
        Add(reika);
    }


    /// <summary>
    /// Aliohjelmalla pystyy "kuuntelijalla" kutsuttaessa kääntämään palloa.
    /// </summary>
    /// <param name="pallo">Käännettävä pallo</param>
    /// <param name="kulma">Pallon kulma</param>
    private void KaannaPalloa(PhysicsObject pallo, Angle kulma)
    {
        pallo.Angle += kulma;
    }


    /// <summary>
    /// Aliohjelma määrittää pallolle vauhdin.
    /// </summary>
    /// <param name="pallo">Pallo, jolle vauhti määritetään</param>
    private void AnnaVauhti(PhysicsObject pallo)
    {
        pelaajanSuunta = Vector.FromLengthAndAngle(700.0, pallo.Angle);
        pallo.MaxVelocity = voimaMittari.Value += 10;
        pallo.LinearDamping = 0.99;
        pallo.AngularDamping = 0.95;
        pallo.Restitution = 0.7;
    }


    /// <summary>
    /// Aliohjelma saa pallon liikkeelle määritetyllä nopeudella.
    /// </summary>
    /// <param name="pallo">Liikuteltava objekti</param>
    private void TyonnaPalloa(PhysicsObject pallo)
    {
        pallo.Hit(pelaajanSuunta);
        voimaMittari.Value = 0;
        lyontiLaskuri.Value += 1;
    }
}
