# HHEM setup: the model files for the NLI check

Output compliance's NLI check scores each sentence of an AI-written answer against the passage it cites, using Vectara HHEM-2.1-Open (Apache-2.0), an ONNX export by Familiar Tools, run inside Isa with ONNX Runtime. No Python, no extra service.

Two files are needed. They are large and stay out of Git (`data/models/` is in .gitignore), so each computer downloads them once.

## Download (PowerShell, from the repo root)

```
mkdir data\models\hhem
curl.exe -L -o data\models\hhem\model.onnx   https://huggingface.co/FamiliarTools/HHEM-2.1-Open-onnx/resolve/main/model.onnx
curl.exe -L -o data\models\hhem\spiece.model https://huggingface.co/google/flan-t5-base/resolve/main/spiece.model
```

`model.onnx` is about 419 MB. `spiece.model` is the flan-t5 SentencePiece tokenizer (under 1 MB). The model repo lists a `sha256.txt` to check the download against.

## Check it works

With `Hhem:SelfTest` true in appsettings.json, the console log at start-up shows one line: a supported sentence should score high (near 1) and an unsupported one low (near 0). If it says "unavailable", the reason is in the log.

## Settings (appsettings.json)

- `Hhem:ModelPath`, `Hhem:TokenizerPath`: where the two files are.
- `Hhem:MaxTokens` (default 1024): a longer passage is cut in half until it fits.
- `OutputCompliance:NliThreshold` (default 0.50): a sentence scoring below this counts as not supported.

## What happens when it is not there

If either file is missing, the NLI check is skipped and the event data says `nli=skipped`. It is never logged as a pass. The other output checks still run.

## Which answers it applies to

AI-written answers only. Direct answers from the database are ISACA's exact words and are not checked. The rehearsed demo questions (`AnswerWording:ExemptQuestions`) keep the old fixed pass.
