use axum::{
    extract::Json,
    response::IntoResponse,
    routing::{get, post},
    Router,
};
use serde::{Deserialize, Serialize};
use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};
use std::net::SocketAddr;

const EMBEDDING_DIM: usize = 384;

#[derive(Deserialize)]
struct EmbedRequest {
    text: String,
}

#[derive(Serialize)]
struct EmbedResponse {
    embedding: Vec<f32>,
    dimensions: usize,
    model: String,
}

#[derive(Serialize)]
struct HealthResponse {
    status: String,
    service: String,
    version: String,
}

async fn health() -> impl IntoResponse {
    Json(HealthResponse {
        status: "healthy".to_string(),
        service: "embeddings-rs".to_string(),
        version: env!("CARGO_PKG_VERSION").to_string(),
    })
}

async fn embed(Json(payload): Json<EmbedRequest>) -> impl IntoResponse {
    let embedding = generate_deterministic_embedding(&payload.text);

    Json(EmbedResponse {
        embedding,
        dimensions: EMBEDDING_DIM,
        model: "deterministic-hash-v1".to_string(),
    })
}

/// Generates a deterministic embedding vector from text using hash-based seeding.
/// This is a stub for development; in production, use a real model (e.g., sentence-transformers).
fn generate_deterministic_embedding(text: &str) -> Vec<f32> {
    let mut hasher = DefaultHasher::new();
    text.hash(&mut hasher);
    let seed = hasher.finish();

    let mut vector: Vec<f32> = Vec::with_capacity(EMBEDDING_DIM);
    let mut state = seed;

    for _ in 0..EMBEDDING_DIM {
        // Simple LCG PRNG for deterministic values
        state = state.wrapping_mul(6364136223846793005).wrapping_add(1442695040888963407);
        let val = ((state >> 33) as f32) / (u32::MAX as f32) * 2.0 - 1.0;
        vector.push(val);
    }

    // L2 normalize
    let magnitude: f32 = vector.iter().map(|v| v * v).sum::<f32>().sqrt();
    if magnitude > 0.0 {
        for v in vector.iter_mut() {
            *v /= magnitude;
        }
    }

    vector
}

#[tokio::main]
async fn main() {
    tracing_subscriber::init();

    let app = Router::new()
        .route("/health", get(health))
        .route("/embed", post(embed));

    let addr = SocketAddr::from(([0, 0, 0, 0], 8900));
    tracing::info!("Embeddings service listening on {}", addr);

    let listener = tokio::net::TcpListener::bind(addr).await.unwrap();
    axum::serve(listener, app).await.unwrap();
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_deterministic_embedding() {
        let text = "hello world";
        let emb1 = generate_deterministic_embedding(text);
        let emb2 = generate_deterministic_embedding(text);

        assert_eq!(emb1.len(), EMBEDDING_DIM);
        assert_eq!(emb1, emb2, "Embeddings should be deterministic");
    }

    #[test]
    fn test_different_texts_produce_different_embeddings() {
        let emb1 = generate_deterministic_embedding("hello");
        let emb2 = generate_deterministic_embedding("world");

        assert_ne!(emb1, emb2, "Different texts should produce different embeddings");
    }

    #[test]
    fn test_embedding_is_normalized() {
        let emb = generate_deterministic_embedding("test normalization");
        let magnitude: f32 = emb.iter().map(|v| v * v).sum::<f32>().sqrt();

        assert!(
            (magnitude - 1.0).abs() < 0.01,
            "Embedding should be L2 normalized, got magnitude {}",
            magnitude
        );
    }

    #[test]
    fn test_empty_text() {
        let emb = generate_deterministic_embedding("");
        assert_eq!(emb.len(), EMBEDDING_DIM);
    }
}
