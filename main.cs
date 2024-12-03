using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class Carta
{
    public int Numero { get; private set; }
    public string Naipe { get; private set; }

    public Carta(int numero, string naipe)
    {
        Numero = numero;
        Naipe = naipe;
    }

    public override string ToString()
    {
        return $"{Numero} de {Naipe}";
    }
}

public class Jogador
{
    public string Nome { get; private set; }
    public int Posicao { get; set; }
    public int QuantidadeCartas => Monte.Count;
    public Queue<int> HistoricoPosicoes { get; private set; }
    public List<Carta> Monte { get; private set; }

    public Jogador(string nome)
    {
        Nome = nome;
        Monte = new List<Carta>();
        HistoricoPosicoes = new Queue<int>();
    }

    public void AdicionarAoHistorico(int posicao)
    {
        if (HistoricoPosicoes.Count == 5)
            HistoricoPosicoes.Dequeue();
        HistoricoPosicoes.Enqueue(posicao);
    }
}

public class Jogo
{
    private List<Carta> MonteDeCompra;
    private List<Carta> AreaDeDescarte;
    private List<Jogador> Jogadores;
    private Random random;
    private StreamWriter logWriter;


    public Jogo(List<Jogador> jogadores, int quantidadeCartas, int partida)
    {
        random = new Random();
        MonteDeCompra = new List<Carta>();
        AreaDeDescarte = new List<Carta>();
        Jogadores = jogadores;

        foreach (var jogador in Jogadores)
        {
            jogador.Monte.Clear();
        }

        logWriter = new StreamWriter($"log_partida_{partida}.txt", append: false);
        logWriter.WriteLine($"O baralho foi criado com {quantidadeCartas} cartas.");
        logWriter.WriteLine("Jogadores da partida: " + string.Join(", ", Jogadores.Select(j => j.Nome)));
        logWriter.WriteLine();

        MonteDeCompra = CriarBaralho(quantidadeCartas);
    }

    private List<Carta> CriarBaralho(int quantidadeCartas)
    {
        var baralho = new List<Carta>();
        string[] naipes = { "Ouros", "Copas", "Espadas", "Paus" };

        for (int numero = 1; numero <= 13; numero++)
        {
            foreach (var naipe in naipes)
            {
                baralho.Add(new Carta(numero, naipe));
                if (baralho.Count == quantidadeCartas)
                {
                    logWriter.WriteLine("O baralho foi embaralhado.");
                    return baralho.OrderBy(c => random.Next()).ToList();
                }
            }
        }

        logWriter.WriteLine("O baralho foi embaralhado.");
        return baralho.OrderBy(c => random.Next()).ToList();
    }

    private List<Jogador> CriarJogadores(int quantidadeJogadores)
    {
        var jogadores = new List<Jogador>();
        for (int i = 0; i < quantidadeJogadores; i++)
        {
            Console.Write($"Digite o nome do jogador {i + 1}: ");
            string nome = Console.ReadLine();
            jogadores.Add(new Jogador(nome));
        }
        return jogadores;
    }

    public void IniciarPartida()
    {
        logWriter.WriteLine("A partida começou.");

        while (MonteDeCompra.Count > 0)
        {
            foreach (var jogador in Jogadores)
            {
                if (MonteDeCompra.Count == 0) break;

                ProcessarJogada(jogador);
            }
        }

        FinalizarPartida();
    }

    private void ProcessarJogada(Jogador jogador)
    {
        while (MonteDeCompra.Count > 0)
        {
            var cartaDaVez = MonteDeCompra.Last();
            MonteDeCompra.RemoveAt(MonteDeCompra.Count - 1);

            logWriter.WriteLine($"{jogador.Nome} comprou a carta {cartaDaVez}.");

            // Regra 1: Roubar monte de outro jogador
            var monteAlvo = Jogadores
                .Where(j => j != jogador && j.Monte.Count > 0)
                .OrderByDescending(j => j.Monte.Count)
                .FirstOrDefault(j => j.Monte.Last().Numero == cartaDaVez.Numero);

            if (monteAlvo != null)
            {
                jogador.Monte.AddRange(monteAlvo.Monte);
                monteAlvo.Monte.Clear();
                jogador.Monte.Add(cartaDaVez);
                logWriter.WriteLine($"{jogador.Nome} roubou o monte de {monteAlvo.Nome}.");
                continue;
            }

            // Regra 2: Roubar da área de descarte
            var cartaDescarte = AreaDeDescarte.FirstOrDefault(c => c.Numero == cartaDaVez.Numero);
            if (cartaDescarte != null)
            {
                AreaDeDescarte.Remove(cartaDescarte);
                jogador.Monte.Add(cartaDescarte);
                jogador.Monte.Add(cartaDaVez);
                logWriter.WriteLine($"{jogador.Nome} pegou uma carta da área de descarte.");
                continue;
            }

            // Regra 3: Adicionar ao próprio monte
            if (jogador.Monte.Count > 0 && jogador.Monte.Last().Numero == cartaDaVez.Numero)
            {
                jogador.Monte.Add(cartaDaVez);
                logWriter.WriteLine($"{jogador.Nome} adicionou a carta ao seu monte.");
                continue;
            }

            // Regra 4: Adicionar à área de descarte
            AreaDeDescarte.Add(cartaDaVez);
            logWriter.WriteLine($"{jogador.Nome} descartou a carta.");
            break;
        }
    }

    public void FinalizarPartida()
    {
        var vencedores = Jogadores.OrderByDescending(j => j.Monte.Count).ToList();
        var maiorQuantidade = vencedores.First().Monte.Count;

        foreach (var vencedor in vencedores.Where(j => j.Monte.Count == maiorQuantidade))
        {
            logWriter.WriteLine($"Vencedor: {vencedor.Nome} com {vencedor.Monte.Count} cartas.");
        }

        logWriter.WriteLine("\nRanking:");
        foreach (var jogador in vencedores)
        {
            logWriter.WriteLine($"{jogador.Nome}: {jogador.Monte.Count} cartas.");
        }

        for (int i = 0; i < vencedores.Count; i++)
        {
            vencedores[i].AdicionarAoHistorico(i + 1);
        }

        logWriter.Close();

        Console.Write("\nDeseja consultar o histórico de posições de um jogador? (S/N): ");
        string resposta = Console.ReadLine()?.Trim().ToUpper();
        if (resposta == "S")
        {
            Console.Write("Digite o nome do jogador: ");
            string nomeJogador = Console.ReadLine();

            var jogadorEncontrado = Jogadores.FirstOrDefault(j => j.Nome.Equals(nomeJogador, StringComparison.OrdinalIgnoreCase));
            if (jogadorEncontrado != null)
            {
                Console.WriteLine($"\nHistórico de posições de {jogadorEncontrado.Nome}:");
                foreach (var posicao in jogadorEncontrado.HistoricoPosicoes)
                {
                    Console.WriteLine($"Posição: {posicao}");
                }
            }
            else
            {
                Console.WriteLine("Jogador não encontrado.");
            }
        }
    }

}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Bem-vindo ao jogo Rouba-Montes!");

        bool jogarNovamente;
        int partida = 1;
        List<Jogador> jogadores = null;

        do
        {
            if (jogadores == null)
            {
                // Escolha do número de jogadores apenas na primeira vez
                Console.Write("Digite a quantidade de jogadores: ");
                int quantidadeJogadores = int.Parse(Console.ReadLine());

                jogadores = new List<Jogador>();
                for (int i = 0; i < quantidadeJogadores; i++)
                {
                    Console.Write($"Digite o nome do jogador {i + 1}: ");
                    string nome = Console.ReadLine();
                    jogadores.Add(new Jogador(nome));
                }
            }

            // Escolha do número de cartas
            Console.Write("Digite a quantidade de cartas no baralho: ");
            int quantidadeCartas = int.Parse(Console.ReadLine());

            // Criação do jogo com os jogadores existentes
            var jogo = new Jogo(jogadores, quantidadeCartas, partida);
            jogo.IniciarPartida();

            // Perguntar se deseja jogar novamente
            Console.Write("Deseja iniciar uma nova partida? (S/N): ");
            string resposta = Console.ReadLine()?.Trim().ToUpper();
            jogarNovamente = resposta == "S";

            partida++;

        } while (jogarNovamente);

        Console.WriteLine("Obrigado por jogar! Até a próxima.");
    }
}