# SuccessFactors-Style HR Core (Sample)

This repository has been rewritten into a small, modular HR system inspired by SAP SuccessFactors domains:

- Employee Central (employee profiles, org assignments)
- Performance & Goals (goals, performance reviews)
- Time Off (leave requests and balances)
- Compensation (salary records)

## Run

```bash
dotnet run --project src\SapSuc.Web\SapSuc.Web.csproj
```

## Notes

This is an educational sample and not production software.

## LLM resilience lab

See [the Vietnamese walkthrough](docs/llm-resilience.md) for configuration, error
contracts and failure experiments. The optional API is disabled by default.

## Embeddings and vector search lab

See [hướng dẫn thực hành tiếng Việt](docs/vector-search.md) for offline embeddings,
cosine top-k, an LSH vector index, exact/ANN recall comparison and an optional
real semantic embedding experiment.


## RAG với trích dẫn nguồn

Xem [hướng dẫn RAG](docs/rag.md): truy xuất chính sách, sinh câu trả lời và kiểm tra trích dẫn theo đoạn/dòng nguồn.
