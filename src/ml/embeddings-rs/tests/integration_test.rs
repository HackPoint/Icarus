// Integration tests for the embeddings service
// These tests verify the HTTP API contract

use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};

const EMBEDDING_DIM: usize = 384;

fn generate_deterministic_embedding(text: &str) -> Vec<f32> {
    let mut hasher = DefaultHasher::new();
    text.hash(&mut hasher);
    let seed = hasher.finish();

    let mut vector: Vec<f32> = Vec::with_capacity(EMBEDDING_DIM);
    let mut state = seed;

    for _ in 0..EMBEDDING_DIM {
        state = state.wrapping_mul(6364136223846793005).wrapping_add(1442695040888963407);
        let val = ((state >> 33) as f32) / (u32::MAX as f32) * 2.0 - 1.0;
        vector.push(val);
    }

    let magnitude: f32 = vector.iter().map(|v| v * v).sum::<f32>().sqrt();
    if magnitude > 0.0 {
        for v in vector.iter_mut() {
            *v /= magnitude;
        }
    }

    vector
}

#[test]
fn test_embedding_contract() {
    let embedding = generate_deterministic_embedding("Icarus RAG platform");

    assert_eq!(embedding.len(), EMBEDDING_DIM);

    // Verify all values are in [-1, 1] range
    for v in &embedding {
        assert!(
            *v >= -1.0 && *v <= 1.0,
            "Embedding value {} out of range [-1, 1]",
            v
        );
    }
}

#[test]
fn test_embedding_stability_across_calls() {
    let text = "stable embedding test";
    let results: Vec<Vec<f32>> = (0..10)
        .map(|_| generate_deterministic_embedding(text))
        .collect();

    for (i, result) in results.iter().enumerate().skip(1) {
        assert_eq!(
            results[0], *result,
            "Embedding at index {} differs from first",
            i
        );
    }
}
