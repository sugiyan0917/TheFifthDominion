using System;                               // .NETの基本機能（例：Random、属性など）を使うための名前空間
using System.Collections.Generic;           // ListやDictionaryなどコレクションを使うための名前空間
using UnityEngine;                          // Unityの型（Vector2、Mathf など）を使うための名前空間

//ノード、ネットワーク
namespace StoryGen.Sim                      // これ以降のクラスを StoryGen.Sim という名前空間にまとめる
{
    public enum NodeRole { Journalist, MainCharacter, Citizen, Historian }  // ノードの役割を列挙型で定義

    [Serializable]                          // Unityのインスペクタで表示/シリアライズ可能にする属性
    public class NodeSim
    {
        public int id;                      // ノード固有のID
        public NodeRole role;               // ノードの役割（記者/主要人物/市民/歴史家）
        public float threshold = 1.0f;      // 感受性のしきい値（小さいほど噂を広めやすい）
        public Vector2 traitWeights = Vector2.one; // 特性の重み（共感度など、ここでは2成分の簡略表現）
        public float score = 0f;            // 受信スコアの累積（一時的な影響量を貯める）
        public string currentRumor = null;  // 現在保持している噂（文字列で表現）
        public System.Random rng;           // ノード個別の乱数生成器（再現性確保用）

        public NodeSim(int id, NodeRole role, float threshold, Vector2 weights, int seed)
        {
            this.id = id;                   // 引数のidをフィールドにセット
            this.role = role;               // 引数のroleをフィールドにセット
            this.threshold = threshold;     // 引数のthresholdをフィールドにセット
            this.traitWeights = weights;    // 引数のweightsをフィールドにセット
            this.rng = new System.Random(seed + id); // ノードごとに異なるシードで乱数器を作成
        }

        public void UpdateThreshold()
        {
            threshold = Mathf.Max(0.05f, threshold - Mathf.Abs(score) * 0.1f);
            // スコアの絶対値に応じてしきい値を下げる（ただし最低0.05まで）
            // → たくさん影響（score）を受けたノードは今後、より噂を受け取りやすくなる

            traitWeights += new Vector2(score * 0.01f, score * 0.01f);
            // スコアに応じて特性重みも少し変化させる（経験により特性が学習されるイメージ）

            score = 0f;                     // このステップで貯めたスコアをリセット（次サイクルに備える）
        }
    }

    public class BeliefNetworkSim
    {
        public readonly Dictionary<int, NodeSim> nodes = new();
        // ノードID → NodeSim の対応表（読み取り専用参照：辞書自体の差し替え不可）

        public readonly List<int>[] adj;    // 隣接リスト：各ノードから出る矢印（from -> to の一覧）
        readonly System.Random rng;         // ネットワーク全体用の乱数生成器

        public BeliefNetworkSim(int numNodes, float th, Vector2 w, float hubPart, int mainCount, int seed = 1234)
        {
            rng = new System.Random(seed);  // ネットワーク用乱数器を初期化（再現性のためseed受け取り）
            adj = new List<int>[numNodes];  // ノード数ぶんの隣接リスト配列を作る
            for (int i = 0; i < numNodes; i++) adj[i] = new List<int>();
            // 各ノードごとに「出力辺の行き先リスト」を空で用意

            for (int i = 0; i < numNodes; i++)
            {
                NodeRole role = NodeRole.Citizen;         // まずは全員を市民とする
                if (i == 0) role = NodeRole.Journalist;   // ID 0 のノードは記者
                else if (i == numNodes - 1) role = NodeRole.Historian;
                // 最後のノード（IDが最大）は歴史家
                else if (i > 0 && i <= mainCount) role = NodeRole.MainCharacter;
                // 1〜mainCount の範囲は主要人物

                nodes[i] = new NodeSim(i, role, th, w, seed);
                // ノードを生成して辞書に登録（全員に同じ初期thresholdとweightsを付与）
            }

            // 記者(0) → 初期エッジ
            int k = Mathf.Max(1, Mathf.RoundToInt(hubPart * numNodes));
            // 記者が初期に繋ぐ先の数を決める（全体のhubPart割合、最低1）
            var candidates = new List<int>();
            for (int i = 1; i < numNodes; i++) candidates.Add(i);
            // 記者以外の全ノードを候補にする

            Shuffle(candidates);             // 候補をランダム順にシャッフル

            for (int c = 0; c < k; c++)
            {
                int t = candidates[c];       // シャッフル後の先頭からk個を取り出し
                AddEdge(0, t);               // 記者(0) → t へエッジを張る（初期拡散の起点づくり）
            }
        }

        public void AddEdge(int from, int to)
        {
            if (from == to) return;          // 自己ループは無視
            if (!adj[from].Contains(to)) adj[from].Add(to);
            // 重複エッジを避けつつ、fromからtoへ有向エッジを追加
        }

        public void UpdateEdgesByActiveRumors(int edgesPerStep = 5)
        {
            var active = new List<int>();
            foreach (var kv in nodes) if (!string.IsNullOrEmpty(kv.Value.currentRumor)) active.Add(kv.Key);
            // 噂を現在保持しているノード（currentRumorが空でない）だけを抽出

            if (active.Count == 0) return;   // アクティブがいなければ何もしない

            int attempts = 0, added = 0;     // 試行回数と、実際に追加できたエッジ数のカウンタ
            while (added < edgesPerStep && attempts < 100)
            {
                int src = active[rng.Next(active.Count)]; // 噂を持つノードからランダムに発信元を選ぶ
                int dst = rng.Next(nodes.Count);          // ランダムな宛先ノードを選ぶ
                if (src != dst && !adj[src].Contains(dst))
                {
                    AddEdge(src, dst);       // 自己ループ・重複でなければエッジ追加
                    added++;                 // 追加数カウント
                }
                attempts++;                  // 試行回数カウント（無限ループ防止）
            }
        }

        void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);     // 0〜i の範囲でランダムな位置を選ぶ
                (list[i], list[j]) = (list[j], list[i]);
                // 要素をスワップ（Fisher–Yatesシャッフルで一様ランダム化）
            }
        }
    }
}
