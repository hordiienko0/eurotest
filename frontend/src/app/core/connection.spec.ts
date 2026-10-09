import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Connection, connectionInterceptor } from './connection';

describe('connectionInterceptor', () => {
  let http: HttpClient;
  let ctrl: HttpTestingController;
  let conn: Connection;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([connectionInterceptor])), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpClient);
    ctrl = TestBed.inject(HttpTestingController);
    conn = TestBed.inject(Connection);
  });

  const respond = (status: number) => {
    http.get('/api/v1/zones').subscribe({ error: () => {} });
    ctrl.expectOne('/api/v1/zones').flush('down', { status, statusText: 'x' });
  };

  it('goes offline when the proxy reports the API unreachable (502)', () => {
    respond(502);
    expect(conn.online()).toBe(false);
  });

  it('stays online on a business error (409)', () => {
    respond(409);
    expect(conn.online()).toBe(true);
  });
});
