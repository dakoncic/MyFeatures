import { environment } from '../environments/environment';
import { Configuration } from '../infrastructure';

export function apiConfigFactory(): Configuration {
  return new Configuration({
    basePath: environment.apiUrl,
  });
}
