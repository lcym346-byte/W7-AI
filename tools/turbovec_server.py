"""
TurboVec 知識庫微服務
用法: pip install turbovec flask
      python tools/turbovec_server.py
"""
from flask import Flask, request, jsonify
from turbovec import IdMapIndex
import numpy as np
import json
import os

app = Flask(__name__)

# 全域索引和文件儲存
DIM = 384  # 預設維度（依 embedding 模型調整）
BIT_WIDTH = 4
INDEX_FILE = "knowledge.tvim"
DOCS_FILE = "knowledge_docs.json"

index = None
docs = {}  # id -> {"text": ..., "source": ...}
next_id = 1

def init():
    global index, docs, next_id
    if os.path.exists(INDEX_FILE):
        index = IdMapIndex.load(INDEX_FILE)
        if os.path.exists(DOCS_FILE):
            with open(DOCS_FILE, "r", encoding="utf-8") as f:
                docs = json.load(f)
        next_id = max([int(k) for k in docs.keys()] + [0]) + 1
    else:
        index = IdMapIndex(bit_width=BIT_WIDTH)

def save():
    index.write(INDEX_FILE)
    with open(DOCS_FILE, "w", encoding="utf-8") as f:
        json.dump(docs, f, ensure_ascii=False, indent=2)

@app.route("/status", methods=["GET"])
def status():
    return jsonify({
        "status": "ok",
        "documents": len(docs),
        "index_size": len(index)
    })

@app.route("/add", methods=["POST"])
def add():
    global next_id
    data = request.json
    vec = np.array(data["vector"], dtype=np.float32).reshape(1, -1)
    doc_id = np.array([next_id], dtype=np.uint64)

    index.add_with_ids(vec, doc_id)
    docs[str(next_id)] = {
        "text": data.get("text", ""),
        "source": data.get("source", "")
    }
    next_id += 1
    save()

    return jsonify({"status": "ok", "id": int(doc_id[0])})

@app.route("/search", methods=["POST"])
def search():
    data = request.json
    vec = np.array(data["vector"], dtype=np.float32).reshape(1, -1)
    k = data.get("k", 5)

    scores, ids = index.search(vec, k=k)

    results_ids = ids[0].tolist()
    results_scores = scores[0].tolist()
    results_texts = []

    for rid in results_ids:
        doc = docs.get(str(rid), {})
        results_texts.append(doc.get("text", ""))

    return jsonify({
        "ids": results_ids,
        "scores": results_scores,
        "texts": results_texts
    })

@app.route("/delete", methods=["POST"])
def delete():
    data = request.json
    doc_id = data["id"]
    index.remove(doc_id)
    docs.pop(str(doc_id), None)
    save()
    return jsonify({"status": "ok"})

if __name__ == "__main__":
    init()
    print("TurboVec Knowledge Server running on http://localhost:5050")
    app.run(host="0.0.0.0", port=5050)
