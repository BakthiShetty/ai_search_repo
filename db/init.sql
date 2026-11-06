CREATE EXTENSION IF NOT EXISTS vector;

-- File metadata (no content stored here)
CREATE TABLE file_metadata (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    filename            TEXT NOT NULL,
    filename_normalized TEXT NOT NULL,
    folder_path         TEXT,
    author              TEXT,
    created_at          TIMESTAMP,
    nas_path            TEXT NOT NULL
);

-- Chunk vectors only — deliberately no 'text' or 'content' column
CREATE TABLE file_chunks (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_id     UUID NOT NULL REFERENCES file_metadata(id) ON DELETE CASCADE,
    chunk_index INT  NOT NULL,
    embedding   VECTOR(768) NOT NULL
);

-- IVFFlat index: only add once the dataset has 1000+ rows.
-- IVFFlat with lists=100 needs ~200 rows minimum; on smaller datasets it
-- probes too few clusters (probes=1 default) and misses most results.
-- CREATE INDEX ON file_chunks
--     USING ivfflat (embedding vector_cosine_ops)
--     WITH (lists = 100);

-- Full-text index on metadata for keyword fallback
CREATE INDEX ON file_metadata
    USING GIN (to_tsvector('english',
        filename_normalized || ' ' || COALESCE(folder_path, '') || ' ' || COALESCE(author, '')));
