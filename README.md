Mid2BMS
=======
This is a tool to help convert .mid to .bms.

これは.midファイルを.bmsに変換するソフトです。  
いろいろと不完全な点はありますが、とりあえず公開します。  
実行ファイルは http://mid2bms.web.fc2.com/ に置いてあります。  
画面右のDownload ZIPからファイルをダウンロードするか、git cloneしてください。  
使い方の詳細は、http://mid2bms.web.fc2.com/ にある「チュートリアル」「よくある質問」「各設定項目の解説」あたりを参照してください。  
**/Mid2BMS/Mid2BMS/_Docs** フォルダにpdfがいくつか置いてありますがかなり古いので当てにしないでください。  

![flowchart_bluemode_2.png](/flowchart_bluemode_2.png)

=======
**Be-Music Helperとのちがい**  
・**オートメーションに部分的に対応**  
・**和音を１つのキー音にまとめる機能に対応**  
・複数トラックを一括変換することができる  
・重複定義機能がある  
・自動ポルタメント(slide/glide)に対応(purpleモード)  
・近しいベロシティを同一とみなす機能に**非対応**  
・音を区切るアルゴリズムに**無音検出**を使用  
・UIがいろいろ違う  
・オープンソ　ース！  
・その他  
  
**注意点**  
・コンプレッサー等を使用した曲には非対応  
・キースイッチ等を使用したインストゥルメントには非対応  
・FL Studio, Reaperには部分的に非対応  
・正しい英語に非対応  
  
=======
ある冬の日、とある発狂皆伝の方が言いました。  
**「BMS界隈は縮小してる」**  
これを聞いた私は恐怖を感じました。  
このままではいけないと感じた私は、  
長年温めてきたBMS作成補助ツールを公開することを決めました。  
このツール及びソースコードが、  
BMS界隈の発展につながることを祈ってやみません。  

About:  
Mid2BMS BMS Inproved Development Environment  
Released on April 1st, 2014  
License: GNU **Lesser** General Public License  
Copyright (c) 2007-2014 yuinore  
(修正版を公開する際はソースコードも公開してください。)  

Thanks:
-----------------------------------
NVorbis  
Copyright (c) 2012 Andrew Ward, Ms-PL  
Microsoft Public License (Ms-PL)  
http://nvorbis.codeplex.com/
-----------------------------------
DynamicJson  
ver 1.2.0.0 (May. 21th, 2010)  
  
created and maintained by neuecc &lt;ils@neue.cc&gt;  
licensed under Microsoft Public License(Ms-PL)  
http://neue.cc/  
http://dynamicjson.codeplex.com/
-----------------------------------
SilverlightやWindows Phoneで各種日本語文字コードを扱う  
License: New BSD License  
Copyright (C) 2011 soramimi (S.Fuchita)  
http://www.soramimi.jp/dotnet/jcode/index.html
-----------------------------------
and so on!
=======
Make BMS Faster, Make BMS Creative.
