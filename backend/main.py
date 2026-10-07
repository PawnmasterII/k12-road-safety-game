"""CUTR K-12 Safety Game — FastAPI backend entry point."""
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from db import init_db
from routers import auth_routes, modules_routes, progress_routes

app = FastAPI(title="CUTR K-12 Safety Game API")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(auth_routes.router)
app.include_router(modules_routes.router)
app.include_router(progress_routes.router)


@app.on_event("startup")
def startup() -> None:
    init_db()


@app.get("/api/health")
def health():
    return {"ok": True}
